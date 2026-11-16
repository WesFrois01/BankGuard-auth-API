using BankGuard_Auth_API.Common;
using BankGuard_Auth_API.Data;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using Microsoft.EntityFrameworkCore;

namespace BankGuard_Auth_API.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly TokenService _tokenService;

    public AuthService(AppDbContext context, TokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<ServiceResult<object>> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return ServiceResult<object>.Fail("Email e senha sao obrigatorios.", 400);

        if (request.Password.Length < 8)
            return ServiceResult<object>.Fail("A senha deve ter no minimo 8 caracteres.", 400);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await _context.Users.AnyAsync(u => u.Email == normalizedEmail))
            return ServiceResult<object>.Fail("Este email ja esta cadastrado.", 409);

        var user = new User
        {
            FullName = request.FullName,
            Email = normalizedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Balance = 1000.00m
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return ServiceResult<object>.Ok(new
        {
            message = "Conta criada com sucesso!",
            userId = user.Id,
            initialBalance = user.Balance
        });
    }

    public async Task<ServiceResult<object>> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ServiceResult<object>.Fail("Credenciais invalidas.", 401);

        var token = _tokenService.GenerateToken(user);

        return ServiceResult<object>.Ok(new
        {
            message = "Login realizado com sucesso!",
            token,
            expiresInMinutes = 30
        });
    }
}