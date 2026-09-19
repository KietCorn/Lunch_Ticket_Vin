using LunchTicket.Api.Repositories;
using LunchTicket.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Tests;

public class LedgerServiceTests
{
    private static (LedgerService service, Data.AppDbContext db) CreateSut()
    {
        var db = TestDbFactory.Create();
        var repository = new LedgerRepository(db);
        return (new LedgerService(repository), db);
    }

    [Fact]
    public async Task DeductBalanceAsync_reduces_balance_and_writes_one_transaction_row()
    {
        var (service, db) = CreateSut();
        var (_, account) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);

        await service.DeductBalanceAsync(account, 30m, referenceOrderId: null, note: "test deduction");
        await db.SaveChangesAsync();

        Assert.Equal(70m, account.Balance);

        var transactions = db.Transactions.ToList();
        var row = Assert.Single(transactions);
        Assert.Equal(-30m, row.Amount);
        Assert.Equal(70m, row.BalanceAfter);
    }

    [Fact]
    public async Task DeductBalanceAsync_throws_and_does_not_mutate_balance_when_insufficient()
    {
        var (service, db) = CreateSut();
        var (_, account) = await TestDbFactory.SeedStudentAsync(db, balance: 10m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeductBalanceAsync(account, 30m, referenceOrderId: null, note: "should fail"));

        Assert.Equal(10m, account.Balance);
        Assert.Empty(db.Transactions);
    }

    [Fact]
    public async Task RefundBalanceAsync_increases_balance_and_writes_one_transaction_row()
    {
        var (service, db) = CreateSut();
        var (_, account) = await TestDbFactory.SeedStudentAsync(db, balance: 50m);

        await service.RefundBalanceAsync(account, 20m, referenceOrderId: 1, note: "refund");
        await db.SaveChangesAsync();

        Assert.Equal(70m, account.Balance);
        var row = Assert.Single(db.Transactions.ToList());
        Assert.Equal(20m, row.Amount);
        Assert.Equal(70m, row.BalanceAfter);
    }

    [Fact]
    public async Task TopUpAsync_rejects_non_positive_amounts()
    {
        var (service, db) = CreateSut();
        await TestDbFactory.SeedStudentAsync(db, balance: 0m);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.TopUpAsync(1, 0m, actorStaffId: 1));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.TopUpAsync(1, -5m, actorStaffId: 1));
    }

    [Fact]
    public void EnforceBalance_throws_when_amount_would_push_balance_negative()
    {
        var (service, db) = CreateSut();
        var account = new Models.Account { Balance = 5m };

        Assert.Throws<InvalidOperationException>(() => service.EnforceBalance(account, 10m));
    }

    [Fact]
    public async Task Database_check_constraint_rejects_a_negative_balance_even_if_app_logic_is_bypassed()
    {
        var db = TestDbFactory.Create();
        var (_, account) = await TestDbFactory.SeedStudentAsync(db, balance: 10m);

        account.Balance = -1m;

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
}
