using LunchTicket.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<Timeslot> Timeslots => Set<Timeslot>();
    public DbSet<DailyMenuEntry> DailyMenuEntries => Set<DailyMenuEntry>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Student>(e =>
        {
            e.HasIndex(s => s.StudentCode).IsUnique();
        });

        modelBuilder.Entity<Account>(e =>
        {
            e.HasIndex(a => a.StudentId).IsUnique();
            e.HasOne(a => a.Student).WithOne(s => s.Account).HasForeignKey<Account>(a => a.StudentId);
            e.ToTable(t => t.HasCheckConstraint("CK_Account_Balance_NonNegative", "\"Balance\" >= 0"));
        });

        modelBuilder.Entity<Card>(e =>
        {
            e.HasIndex(c => c.CardToken).IsUnique();
            e.Property(c => c.Status).HasConversion<string>();
            e.HasOne(c => c.Student).WithMany(s => s.Cards).HasForeignKey(c => c.StudentId);
        });

        modelBuilder.Entity<Staff>(e =>
        {
            e.HasIndex(s => s.Username).IsUnique();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.Property(t => t.TransactionType).HasConversion<string>();
            e.HasOne(t => t.Account).WithMany(a => a.Transactions).HasForeignKey(t => t.AccountId);
        });

        modelBuilder.Entity<DailyMenuEntry>(e =>
        {
            e.HasIndex(d => new { d.Date, d.TimeslotId, d.MenuItemId }).IsUnique();
            e.HasOne(d => d.Timeslot).WithMany().HasForeignKey(d => d.TimeslotId);
            e.HasOne(d => d.MenuItem).WithMany(m => m.DailyMenuEntries).HasForeignKey(d => d.MenuItemId);
            e.ToTable(t => t.HasCheckConstraint("CK_DailyMenu_Quantity_NonNegative", "\"AvailableQuantity\" >= 0"));
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasIndex(o => o.QrToken).IsUnique();
            e.Property(o => o.OrderType).HasConversion<string>();
            e.Property(o => o.Status).HasConversion<string>();
            e.Property(o => o.Source).HasConversion<string>();
            e.HasOne(o => o.Student).WithMany(s => s.Orders).HasForeignKey(o => o.StudentId);
            e.HasOne(o => o.DailyMenu).WithMany().HasForeignKey(o => o.DailyMenuId);
            e.HasOne(o => o.Timeslot).WithMany().HasForeignKey(o => o.TimeslotId);
        });
    }
}
