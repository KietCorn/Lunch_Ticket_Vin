using LunchTicket.Api.Models;

namespace LunchTicket.Api.Repositories;

public interface ILedgerRepository
{
    Task<Account?> GetAccountByStudentIdAsync(int studentId);
    Task<List<Transaction>> GetTransactionsByStudentIdAsync(int studentId);
    Task<List<Transaction>> GetAllTransactionsAsync(DateTime? from, DateTime? to);
    Task AddTransactionAsync(Transaction transaction);
    Task AddAccountAsync(Account account);
}
