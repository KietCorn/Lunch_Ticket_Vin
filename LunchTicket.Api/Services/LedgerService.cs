using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;

namespace LunchTicket.Api.Services;

public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _ledgerRepository;

    public LedgerService(ILedgerRepository ledgerRepository)
    {
        _ledgerRepository = ledgerRepository;
    }

    public async Task<AccountDto> GetBalanceAsync(int studentId)
    {
        var account = await _ledgerRepository.GetAccountByStudentIdAsync(studentId)
            ?? throw new KeyNotFoundException($"No account for student {studentId}.");
        return ToDto(account);
    }

    public async Task<List<TransactionDto>> GetTransactionsAsync(int studentId)
    {
        var transactions = await _ledgerRepository.GetTransactionsByStudentIdAsync(studentId);
        return transactions.Select(ToDto).ToList();
    }

    public async Task<List<TransactionDto>> GetAllTransactionsAsync(DateTime? from, DateTime? to)
    {
        var transactions = await _ledgerRepository.GetAllTransactionsAsync(from, to);
        return transactions.Select(ToDto).ToList();
    }

    public async Task<AccountDto> TopUpAsync(int studentId, decimal amount, int actorStaffId)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Top-up amount must be positive.");

        var account = await _ledgerRepository.GetAccountByStudentIdAsync(studentId)
            ?? throw new KeyNotFoundException($"No account for student {studentId}.");

        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _ledgerRepository.AddTransactionAsync(new Transaction
        {
            AccountId = account.Id,
            TransactionType = TransactionType.TopUp,
            Amount = amount,
            BalanceAfter = account.Balance,
            ActorId = actorStaffId,
            CreatedAt = DateTime.UtcNow
        });

        return ToDto(account);
    }

    public void EnforceBalance(Account account, decimal amount)
    {
        if (account.Balance - amount < 0)
            throw new InvalidOperationException("INSUFFICIENT_BALANCE");
    }

    public async Task DeductBalanceAsync(Account account, decimal amount, int? referenceOrderId, string? note)
    {
        EnforceBalance(account, amount);

        account.Balance -= amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _ledgerRepository.AddTransactionAsync(new Transaction
        {
            AccountId = account.Id,
            TransactionType = TransactionType.Deduction,
            Amount = -amount,
            BalanceAfter = account.Balance,
            ReferenceOrderId = referenceOrderId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });
    }

    public async Task RefundBalanceAsync(Account account, decimal amount, int? referenceOrderId, string? note)
    {
        account.Balance += amount;
        account.UpdatedAt = DateTime.UtcNow;

        await _ledgerRepository.AddTransactionAsync(new Transaction
        {
            AccountId = account.Id,
            TransactionType = TransactionType.Refund,
            Amount = amount,
            BalanceAfter = account.Balance,
            ReferenceOrderId = referenceOrderId,
            Note = note,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static AccountDto ToDto(Account a) => new(a.Id, a.StudentId, a.Balance, a.UpdatedAt);

    private static TransactionDto ToDto(Transaction t) => new(
        t.Id, t.AccountId, t.TransactionType.ToString(), t.Amount, t.BalanceAfter,
        t.ReferenceOrderId, t.ActorId, t.Note, t.CreatedAt);
}
