using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    public static readonly List<AuditLog> AuditLogs = new();

    [HttpPost("transfer")]
    public IActionResult Transfer([FromBody] TransferRequest request)
    {
        var idClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(idClaim, out Guid senderId))
        {
            return Unauthorized(new { message = "Token malformado." });
        }

        var sender = AuthController.Users.FirstOrDefault(u => u.Id == senderId);
        if (sender == null)
        {
            return NotFound(new { message = "Conta de origem nao encontrada." });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { message = "Valor deve ser maior que zero." });
        }

        var recipient = AuthController.Users.FirstOrDefault(u =>
            u.Email.Equals(request.RecipientEmail, StringComparison.OrdinalIgnoreCase));

        if (recipient == null)
        {
            var log = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = request.RecipientEmail,
                Amount = request.Amount,
                Status = "BLOCKED",
                Reason = "Destinatario inexistente"
            };
            AuditLogs.Add(log);
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
            AuditLogs.Add(log);
            return BadRequest(new { message = "Saldo insuficiente.", auditId = log.Id });
        }

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
        AuditLogs.Add(okLog);

        return Ok(new TransferResponse(
            "Transferencia realizada com sucesso!",
            sender.Balance,
            okLog.Id
        ));
    }

    [HttpGet("audit")]
    public IActionResult GetAuditLogs()
    {
        return Ok(AuditLogs.OrderByDescending(l => l.TimestampUtc));
    }
}