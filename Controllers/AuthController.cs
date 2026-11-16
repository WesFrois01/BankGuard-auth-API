using BankGuard_Auth_API.Data;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using BankGuard_Auth_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthController(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email e senha sao obrigatorios." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "A senha deve ter no minimo 8 caracteres." });
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
        {
            return Conflict(new { message = "Este email ja esta cadastrado." });
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 1000.00m
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Conta criada com sucesso!",
            userId = user.Id,
            initialBalance = user.Balance
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Credenciais invalidas." });
        }

        var token = _tokenService.GenerateToken(user);

        return Ok(new
        {
            message = "Login realizado com sucesso!",
            token,
            expiresInMinutes = 30
        });
    }
}
