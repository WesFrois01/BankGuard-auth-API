using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        var idClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                   ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!Guid.TryParse(idClaim, out Guid senderId))
            return Unauthorized(new { message = "Token malformado." });

        var result = await _transactionService.TransferAsync(senderId, request);

        if (!result.IsSuccess)
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });

        return StatusCode(result.StatusCode, result.Data);
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs()
    {
        var logs = await _transactionService.GetAuditLogsAsync();
        return Ok(logs);
    }
}