using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;

namespace LunchTicket.Api.Services;

public interface ILedgerService
{
    Task<AccountDto> GetBalanceAsync(int studentId);
    Task<List<TransactionDto>> GetTransactionsAsync(int studentId);
    Task<List<TransactionDto>> GetAllTransactionsAsync(DateTime? from, DateTime? to);
    Task<AccountDto> TopUpAsync(int studentId, decimal amount, int actorStaffId);

    /// <summary>Throws InvalidOperationException if the deduction would drive the balance negative. Caller controls the transaction/SaveChanges boundary.</summary>
    Task DeductBalanceAsync(Account account, decimal amount, int? referenceOrderId, string? note);
    Task RefundBalanceAsync(Account account, decimal amount, int? referenceOrderId, string? note);
    void EnforceBalance(Account account, decimal amount);
}
