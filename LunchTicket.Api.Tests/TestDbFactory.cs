using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Tests;

/// <summary>
/// Builds an AppDbContext backed by an in-memory SQLite database (not the EF InMemory
/// provider) so CHECK constraints, transactions, and unique indexes behave exactly like
/// the real sqlite file the app ships with.
/// </summary>
public static class TestDbFactory
{
    public static AppDbContext Create() => Create(out _);

    public static AppDbContext Create(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var db = CreateContext(connection);
        db.Database.EnsureCreated();
        return db;
    }

    /// <summary>
    /// Opens a second, independent AppDbContext against the same in-memory connection so a
    /// test can verify what was actually persisted, bypassing the first context's change
    /// tracker (which would otherwise show in-memory mutations that a rolled-back
    /// transaction never wrote to disk).
    /// </summary>
    public static AppDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        return new AppDbContext(options);
    }

    public static async Task<(Student student, Account account)> SeedStudentAsync(AppDbContext db, decimal balance, string code = "S001")
    {
        var student = new Student { StudentCode = code, FullName = "Test Student", CreatedAt = DateTime.UtcNow };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var account = new Account { StudentId = student.Id, Balance = balance };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return (student, account);
    }

    public static async Task<DailyMenuEntry> SeedDailyMenuEntryAsync(AppDbContext db, decimal price, int quantity, string date = "2026-09-20")
    {
        var menuItem = new MenuItem { Name = "Test Meal", Price = price, IsActive = true };
        db.MenuItems.Add(menuItem);

        var timeslot = new Timeslot { Label = "Lunch", StartTime = "12:00", EndTime = "13:00", SortOrder = 1 };
        db.Timeslots.Add(timeslot);
        await db.SaveChangesAsync();

        var entry = new DailyMenuEntry
        {
            Date = date,
            TimeslotId = timeslot.Id,
            MenuItemId = menuItem.Id,
            AvailableQuantity = quantity
        };
        db.DailyMenuEntries.Add(entry);
        await db.SaveChangesAsync();

        return entry;
    }
}
