using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using BankGuard_Auth_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
 
namespace BankGuard_Auth_API.Controllers;
 
[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    public static readonly List<AuditLog> AuditLogs = new();
 
    private ClaimsPrincipal? ValidateToken()
    {
        string auth = Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer "))
        {
            return null;
        }
 
        string token = auth.Substring(7).Trim();
        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(TokenService.SecretKey);
 
        try
        {
            return handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "BankGuard.Auth.API",
                ValidateAudience = true,
                ValidAudience = "BankGuard.Clients",
                ClockSkew = TimeSpan.Zero
            }, out _);
        }
        catch
        {
            return null;
        }
    }
 
    [HttpPost("transfer")]
    public IActionResult Transfer([FromBody] TransferRequest request)
    {
        var userClaims = ValidateToken();
        if (userClaims == null)
        {
            return Unauthorized(new { message = "Token JWT invalido ou ausente." });
        }
 
        var idClaim = userClaims.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? userClaims.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 
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
        if (ValidateToken() == null)
        {
            return Unauthorized(new { message = "Token JWT necessario." });
        }
 
        return Ok(AuditLogs.OrderByDescending(l => l.TimestampUtc));
    }
}
 