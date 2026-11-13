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
    private readonly TokenService _tokenService = new();

    [HttpPost("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8)
            return BadRequest(new { message = "E-mail inválido ou senha com menos de 8 caracteres." });

        if (Users.Any(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Este e-mail já está cadastrado." });

        string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = passwordHash
        };

        Users.Add(newUser);

        var response = new UserResponse(newUser.Id, newUser.FullName, newUser.Email, newUser.Balance);

        return Created("", new
        {
            message = "Conta criada com segurança!",
            user = response,
            hashGerado = passwordHash
        });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var user = Users.FirstOrDefault(u => u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase));

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Credenciais inválidas." });

        var (token, expiresAt) = _tokenService.GenerateToken(user);

        var userResponse = new UserResponse(user.Id, user.FullName, user.Email, user.Balance);

        return Ok(new LoginResponse(token, expiresAt, userResponse));
    }
}
