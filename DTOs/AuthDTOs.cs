namespace BankGuard_Auth_API.DTOs;

public record RegisterRequest(string FullName, string Email, string Password);

public record UserResponse(Guid Id, string FullName, string Email, decimal Balance);
