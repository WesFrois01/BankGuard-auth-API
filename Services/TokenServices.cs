using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankGuard_Auth_API.Models;
using Microsoft.IdentityModel.Tokens;

namespace BankGuard_Auth_API.Services;

public class TokenService
{
    public const string SecretKey = "BankGuard_SuperSecret_SigningKey_2026_Minimum32Chars!";

    public (string token, DateTime expiresAt) GenerateToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("fullName", user.FullName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        var token = new JwtSecurityToken(
            issuer: "BankGuard.Auth.API",
            audience: "BankGuard.Clients",
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
