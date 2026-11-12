using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using Microsoft.AspNetCore.Mvc;

namespace BankGuard_Auth_API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    public static readonly List<User> Users = new();

    [HttpPost ("register")]
    public IActionResult Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || request.Password.Length < 8)
            return BadRequest(new { message = "Este e-mail já está cadastrado."});
        
        string PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var newUser = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = PasswordHash
        };

        Users.Add(newUser);

        var response = new UserResponse(newUser.Id, newUser.FullName, newUser.Email, newUser.Balance);

        return Created ("", new
        {
            message = "Conta Criada com segurança!",
            user = response,
            hashGerado = PasswordHash
        });
    }
}