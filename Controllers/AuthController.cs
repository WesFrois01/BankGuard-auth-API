using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using BankGuard_Auth_API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public static readonly List<User> Users = new();
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email e senha sao obrigatorios." });
        }

        if (request.Password.Length < 8)
        {
            return BadRequest(new { message = "A senha deve ter no minimo 8 caracteres." });
        }

        if (Users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
        {
            return Conflict(new { message = "Este email ja esta cadastrado." });
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 1000.00m
        };

        Users.Add(user);

        return Ok(new
        {
            message = "Conta criada com sucesso!",
            userId = user.Id,
            initialBalance = user.Balance
        });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = Users.FirstOrDefault(u =>
            u.Email.Equals(request.Email.Trim(), StringComparison.OrdinalIgnoreCase));

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