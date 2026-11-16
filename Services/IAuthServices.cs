using BankGuard_Auth_API.Common;
using BankGuard_Auth_API.DTOs;

namespace BankGuard_Auth_API.Services;

public interface IAuthService
{
    Task<ServiceResult<object>> RegisterAsync(RegisterRequest request);
    Task<ServiceResult<object>> LoginAsync(LoginRequest request);
}