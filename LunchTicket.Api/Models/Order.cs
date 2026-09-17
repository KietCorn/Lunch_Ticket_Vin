namespace LunchTicket.Api.Models;

public enum OrderType
{
    PreOrder,
    WalkIn
}

public enum OrderStatus
{
    Pending,
    Ready,
    Delivered,
    Cancelled
}

public enum OrderSource
{
    QrScan,
    ManualEntry
}

public class Order
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int DailyMenuId { get; set; }
    public int TimeslotId { get; set; }
    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }
    public OrderSource? Source { get; set; }
    public decimal AmountCharged { get; set; }
    public string? QrToken { get; set; }
    public DateTime? QrExpiresAt { get; set; }
    public int? PlacedByStaffId { get; set; }
    public int? DeliveredById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Student Student { get; set; } = null!;
    public DailyMenuEntry DailyMenu { get; set; } = null!;
    public Timeslot Timeslot { get; set; } = null!;
}
