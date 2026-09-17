namespace LunchTicket.Api.Models;

public enum TransactionType
{
    TopUp,
    Deduction,
    Refund
}

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public TransactionType TransactionType { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public int? ReferenceOrderId { get; set; }
    public int? ActorId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }

    public Account Account { get; set; } = null!;
}
