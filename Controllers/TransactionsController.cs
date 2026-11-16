using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BankGuard_Auth_API.Data;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TransactionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var idClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(idClaim, out Guid senderId))
        {
            return Unauthorized(new { message = "Token malformado." });
        }

        var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
        if (sender == null)
        {
            return NotFound(new { message = "Conta de origem nao encontrada." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Valor deve ser maior que zero." });
        }

        var normalizedRecipientEmail = request.RecipientEmail.Trim().ToLowerInvariant();
        var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedRecipientEmail);

        if (recipient == null)
        {
            var log = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = normalizedRecipientEmail,
                Amount = request.Amount,
                Status = "BLOCKED",
                Reason = "Destinatario inexistente"
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return NotFound(new { message = "Destinatario nao encontrado.", auditId = log.Id });
        }

        if (recipient.Id == sender.Id)
        {
            return BadRequest(new { message = "Nao pode transferir para si mesmo." });
        }

        if (sender.Balance < request.Amount)
        {
            var log = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = recipient.Email,
                Amount = request.Amount,
                Status = "BLOCKED",
                Reason = "Saldo insuficiente"
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return BadRequest(new { message = "Saldo insuficiente.", auditId = log.Id });
        }

        // Inicio da Transacao Atomica (ACID)
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            sender.Balance -= request.Amount;
            recipient.Balance += request.Amount;

            var okLog = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = recipient.Email,
                Amount = request.Amount,
                Status = "SUCCESS",
                Reason = "Transferencia concluida"
            };
            _context.AuditLogs.Add(okLog);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new TransferResponse(
                "Transferencia realizada com sucesso!",
                sender.Balance,
                okLog.Id
            ));
        }
        catch
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "Falha interna ao processar transferencia. Operacao revertida." });
        }
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.TimestampUtc)
            .ToListAsync();

        return Ok(logs);
    }
}