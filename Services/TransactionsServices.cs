using BankGuard_Auth_API.Common;
using BankGuard_Auth_API.Data;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using Microsoft.EntityFrameworkCore;

namespace BankGuard_Auth_API.Services;

public class TransactionService : ITransactionService
{
    private readonly AppDbContext _context;

    public TransactionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<TransferResponse>> TransferAsync(Guid senderId, TransferRequest request)
    {
        var sender = await _context.Users.FirstOrDefaultAsync(u => u.Id == senderId);
        if (sender == null)
            return ServiceResult<TransferResponse>.Fail("Conta de origem nao encontrada.", 404);

        if (request.Amount <= 0)
            return ServiceResult<TransferResponse>.Fail("Valor deve ser maior que zero.", 400);

        var normalizedRecipientEmail = request.RecipientEmail.Trim().ToLowerInvariant();
        var recipient = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedRecipientEmail);

        if (recipient == null)
        {
            var log = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = normalizedRecipientEmail,
                Amount = request.Amount,
                Status = "BLOCKED",
                Reason = "Destinatario inexistente"
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return ServiceResult<TransferResponse>.Fail($"Destinatario nao encontrado. AuditId: {log.Id}", 404);
        }

        if (recipient.Id == sender.Id)
            return ServiceResult<TransferResponse>.Fail("Nao pode transferir para si mesmo.", 400);

        if (sender.Balance < request.Amount)
        {
            var log = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = recipient.Email,
                Amount = request.Amount,
                Status = "BLOCKED",
                Reason = "Saldo insuficiente"
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();

            return ServiceResult<TransferResponse>.Fail($"Saldo insuficiente. AuditId: {log.Id}", 400);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            sender.Balance -= request.Amount;
            recipient.Balance += request.Amount;

            var okLog = new AuditLog
            {
                SenderId = sender.Id,
                SenderEmail = sender.Email,
                RecipientEmail = recipient.Email,
                Amount = request.Amount,
                Status = "SUCCESS",
                Reason = "Transferencia concluida"
            };
            _context.AuditLogs.Add(okLog);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return ServiceResult<TransferResponse>.Ok(new TransferResponse(
                "Transferencia realizada com sucesso!",
                sender.Balance,
                okLog.Id
            ));
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // Repassa para o GlobalExceptionHandler tratar de forma padronizada
        }
    }

 public async Task<PagedResponse<AuditLog>> GetAuditLogsAsync(int page = 1, int pageSize = 10, string? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 50) pageSize = 50; // Proteção contra consultas gigantes

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToUpperInvariant();
            query = query.Where(l => l.Status == normalizedStatus);
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .OrderByDescending(l => l.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<AuditLog>(items, page, pageSize, totalItems, totalPages);
    }
}