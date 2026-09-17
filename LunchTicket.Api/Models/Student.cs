namespace LunchTicket.Api.Models;

public class Student
{
    public int Id { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Account? Account { get; set; }
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}

public class Account
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public decimal Balance { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}

public enum CardStatus
{
    Active,
    Locked
}

public class Card
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string CardToken { get; set; } = string.Empty;
    public CardStatus Status { get; set; }
    public int FailedScanCount { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime? LockedAt { get; set; }

    public Student Student { get; set; } = null!;
}
