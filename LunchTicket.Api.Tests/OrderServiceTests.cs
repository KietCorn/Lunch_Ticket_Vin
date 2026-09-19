using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;
using LunchTicket.Api.Services;
using Microsoft.Data.Sqlite;

namespace LunchTicket.Api.Tests;

public class OrderServiceTests
{
    private static (OrderService service, AppDbContext db, SqliteConnection connection) CreateSut()
    {
        var db = TestDbFactory.Create(out var connection);

        var orderRepository = new OrderRepository(db);
        var menuRepository = new MenuRepository(db);
        var ledgerRepository = new LedgerRepository(db);
        var cardRepository = new CardRepository(db);
        var ledgerService = new LedgerService(ledgerRepository);
        var notificationService = new NotificationService();
        var menuService = new MenuService(menuRepository, orderRepository, ledgerService, ledgerRepository, notificationService);
        var cardFraudService = new CardFraudService(db, cardRepository, ledgerRepository, new FakeJwtTokenService());

        var service = new OrderService(
            db, orderRepository, menuRepository, ledgerRepository,
            ledgerService, menuService, cardFraudService, cardRepository, notificationService);

        return (service, db, connection);
    }

    [Fact]
    public async Task PlacePreOrderAsync_deducts_balance_and_decrements_quantity_atomically()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 25m, quantity: 5);

        var order = await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);

        Assert.Equal("Pending", order.Status);
        Assert.Equal(25m, order.AmountCharged);
        Assert.NotNull(order.QrToken);

        var account = db.Accounts.Single(a => a.StudentId == student.Id);
        Assert.Equal(75m, account.Balance);

        var refreshedEntry = db.DailyMenuEntries.Single(d => d.Id == entry.Id);
        Assert.Equal(4, refreshedEntry.AvailableQuantity);

        Assert.Single(db.Transactions.ToList());
    }

    [Fact]
    public async Task PlacePreOrderAsync_rolls_back_quantity_decrement_when_balance_is_insufficient()
    {
        var (service, db, connection) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 5m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 25m, quantity: 5);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId));

        // Verify against a fresh context (not the one whose transaction failed) that nothing
        // was actually persisted: quantity untouched, balance untouched, no order/transaction rows.
        using var verifyDb = TestDbFactory.CreateContext(connection);
        Assert.Equal(5, verifyDb.DailyMenuEntries.Single(d => d.Id == entry.Id).AvailableQuantity);
        Assert.Equal(5m, verifyDb.Accounts.Single(a => a.StudentId == student.Id).Balance);
        Assert.Empty(verifyDb.Orders.ToList());
        Assert.Empty(verifyDb.Transactions.ToList());
    }

    [Fact]
    public async Task PlacePreOrderAsync_throws_out_of_stock_and_does_not_touch_balance()
    {
        var (service, db, connection) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 25m, quantity: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId));

        using var verifyDb = TestDbFactory.CreateContext(connection);
        Assert.Equal(100m, verifyDb.Accounts.Single(a => a.StudentId == student.Id).Balance);
        Assert.Empty(verifyDb.Transactions.ToList());
    }

    [Fact]
    public async Task PlacePreOrderAsync_rejects_a_second_order_for_the_same_student_and_slot()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 10m, quantity: 5);

        await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId));
        Assert.Equal("DUPLICATE_ORDER_FOR_SLOT", ex.Message);
    }

    [Fact]
    public async Task CancelPreOrderAsync_refunds_full_amount_when_cancelled_well_before_the_cutoff()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var farFuture = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd");
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 20m, quantity: 3, date: farFuture);

        var order = await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);

        var result = await service.CancelPreOrderAsync(order.Id);

        Assert.Equal(20m, result.RefundAmount);
        Assert.Equal(100, result.RefundPercent);
        Assert.Equal(100m, db.Accounts.Single(a => a.StudentId == student.Id).Balance);
        Assert.Equal(3, db.DailyMenuEntries.Single(d => d.Id == entry.Id).AvailableQuantity);
    }

    [Fact]
    public async Task CancelPreOrderAsync_refunds_half_when_cancelled_after_the_cutoff()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var pastDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 20m, quantity: 3, date: pastDate);

        var order = await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);

        var result = await service.CancelPreOrderAsync(order.Id);

        Assert.Equal(10m, result.RefundAmount);
        Assert.Equal(50, result.RefundPercent);
        Assert.Equal(90m, db.Accounts.Single(a => a.StudentId == student.Id).Balance);
    }

    [Fact]
    public async Task CancelPreOrderAsync_rejects_an_already_cancelled_order()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 20m, quantity: 3);

        var order = await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);
        await service.CancelPreOrderAsync(order.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CancelPreOrderAsync(order.Id));
    }

    [Fact]
    public async Task ConfirmPreOrderAsync_marks_delivered_and_invalidates_the_qr_token_for_reuse()
    {
        var (service, db, _) = CreateSut();
        var (student, _) = await TestDbFactory.SeedStudentAsync(db, balance: 100m);
        var entry = await TestDbFactory.SeedDailyMenuEntryAsync(db, price: 20m, quantity: 3);
        var order = await service.PlacePreOrderAsync(student.Id, entry.Id, entry.TimeslotId);
        var qrToken = order.QrToken!;

        var confirmed = await service.ConfirmPreOrderAsync(qrToken, staffId: 1);
        Assert.Equal("Delivered", confirmed.Status);
        Assert.Null(confirmed.QrToken);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ConfirmPreOrderAsync(qrToken, staffId: 1));
    }
}
