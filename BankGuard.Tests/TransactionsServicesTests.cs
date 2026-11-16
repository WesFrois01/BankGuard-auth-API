using BankGuard_Auth_API.Data;
using BankGuard_Auth_API.DTOs;
using BankGuard_Auth_API.Models;
using BankGuard_Auth_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BankGuard.Tests;

public class TransactionServiceTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task TransferAsync_ValidTransfer_ShouldUpdateBalancesAndCreateAuditLog()
    {
        // Arrange (Preparação)
        await using var context = CreateInMemoryContext();
        var sender = new User { Email = "alice@bank.com", FullName = "Alice", PasswordHash = "hash", Balance = 1000m };
        var recipient = new User { Email = "bob@bank.com", FullName = "Bob", PasswordHash = "hash", Balance = 500m };

        context.Users.AddRange(sender, recipient);
        await context.SaveChangesAsync();

        var service = new TransactionService(context);
        var request = new TransferRequest("bob@bank.com", 250m);

        // Act (Ação)
        var result = await service.TransferAsync(sender.Id, request);

        // Assert (Validação)
        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal(750m, sender.Balance);
        Assert.Equal(750m, recipient.Balance);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("SUCCESS", auditLog.Status);
        Assert.Equal(250m, auditLog.Amount);
    }

    [Fact]
    public async Task TransferAsync_InsufficientBalance_ShouldFailAndBlockTransaction()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var sender = new User { Email = "alice@bank.com", FullName = "Alice", PasswordHash = "hash", Balance = 100m };
        var recipient = new User { Email = "bob@bank.com", FullName = "Bob", PasswordHash = "hash", Balance = 500m };

        context.Users.AddRange(sender, recipient);
        await context.SaveChangesAsync();

        var service = new TransactionService(context);
        var request = new TransferRequest("bob@bank.com", 300m);

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(100m, sender.Balance); // Saldo permanece intacto
        Assert.Equal(500m, recipient.Balance);

        var auditLog = await context.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(auditLog);
        Assert.Equal("BLOCKED", auditLog.Status);
        Assert.Equal("Saldo insuficiente", auditLog.Reason);
    }

    [Fact]
    public async Task TransferAsync_TransferToSelf_ShouldReturnBadRequest()
    {
        // Arrange
        await using var context = CreateInMemoryContext();
        var sender = new User { Email = "alice@bank.com", FullName = "Alice", PasswordHash = "hash", Balance = 1000m };
        context.Users.Add(sender);
        await context.SaveChangesAsync();

        var service = new TransactionService(context);
        var request = new TransferRequest("alice@bank.com", 100m);

        // Act
        var result = await service.TransferAsync(sender.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal(1000m, sender.Balance);
    }
}