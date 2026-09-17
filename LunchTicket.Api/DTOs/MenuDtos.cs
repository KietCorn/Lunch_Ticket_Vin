namespace LunchTicket.Api.DTOs;

public record MenuItemDto(int Id, string Name, string? Description, decimal Price, bool IsActive);

public record TimeslotDto(int Id, string Label, string StartTime, string EndTime, int SortOrder);

public record DailyMenuEntryDto(int Id, string Date, int TimeslotId, int MenuItemId, int AvailableQuantity);

public record ReportOutOfStockResult(DailyMenuEntryDto DailyMenuEntry, int AffectedOrdersRefunded, int StudentsNotified);
