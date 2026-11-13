namespace BankGuard_Auth_API.DTOs;

public record RegisterRequest(string FullName, string Email, string Password);

public record LoginRequest(string Email, string Password);

public record UserResponse(Guid Id, string FullName, string Email, decimal Balance);

public record LoginResponse(string Token, DateTime ExpiresAt, UserResponse User);

public record TransferRequest(string RecipientEmail, decimal Amount);

public record TransferResponse(string Message, decimal NewBalance, Guid AuditId);