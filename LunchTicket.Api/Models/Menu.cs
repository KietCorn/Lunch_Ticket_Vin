namespace LunchTicket.Api.Models;

public class MenuItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DailyMenuEntry> DailyMenuEntries { get; set; } = new List<DailyMenuEntry>();
}

public class Timeslot
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class DailyMenuEntry
{
    public int Id { get; set; }
    public string Date { get; set; } = string.Empty;
    public int TimeslotId { get; set; }
    public int MenuItemId { get; set; }
    public int AvailableQuantity { get; set; }

    public Timeslot Timeslot { get; set; } = null!;
    public MenuItem MenuItem { get; set; } = null!;
}
