using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Repositories;

public class LedgerRepository : ILedgerRepository
{
    private readonly AppDbContext _db;

    public LedgerRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Account?> GetAccountByStudentIdAsync(int studentId) =>
        _db.Accounts.FirstOrDefaultAsync(a => a.StudentId == studentId);

    public Task<List<Transaction>> GetTransactionsByStudentIdAsync(int studentId) =>
        _db.Transactions
            .Where(t => t.Account.StudentId == studentId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public Task<List<Transaction>> GetAllTransactionsAsync(DateTime? from, DateTime? to) =>
        _db.Transactions
            .Where(t => (!from.HasValue || t.CreatedAt >= from) && (!to.HasValue || t.CreatedAt <= to))
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

    public async Task AddTransactionAsync(Transaction transaction) => await _db.Transactions.AddAsync(transaction);

    public async Task AddAccountAsync(Account account) => await _db.Accounts.AddAsync(account);
}
