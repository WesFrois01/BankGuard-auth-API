using BankGuard_Auth_API.Common;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;

namespace BankGuard_Auth_API.Services;

public interface ITransactionService
{
    Task<ServiceResult<TransferResponse>> TransferAsync(Guid senderId, TransferRequest request);
    Task<PagedResponse<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 10, string? status = null);
}