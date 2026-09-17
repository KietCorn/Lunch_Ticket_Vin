using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;

namespace LunchTicket.Api.Services;

public class MenuService : IMenuService
{
    private readonly IMenuRepository _menuRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILedgerService _ledgerService;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly INotificationService _notificationService;

    public MenuService(
        IMenuRepository menuRepository,
        IOrderRepository orderRepository,
        ILedgerService ledgerService,
        ILedgerRepository ledgerRepository,
        INotificationService notificationService)
    {
        _menuRepository = menuRepository;
        _orderRepository = orderRepository;
        _ledgerService = ledgerService;
        _ledgerRepository = ledgerRepository;
        _notificationService = notificationService;
    }

    public async Task<List<MenuItemDto>> GetMenuItemsAsync()
    {
        var items = await _menuRepository.GetMenuItemsAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<MenuItemDto> CreateMenuItemAsync(string name, string? description, decimal price)
    {
        var item = new MenuItem { Name = name, Description = description, Price = price, IsActive = true };
        await _menuRepository.AddMenuItemAsync(item);
        return ToDto(item);
    }

    public async Task<List<TimeslotDto>> GetTimeslotsAsync()
    {
        var slots = await _menuRepository.GetTimeslotsAsync();
        return slots.Select(ToDto).ToList();
    }

    public async Task<List<DailyMenuEntryDto>> GetDailyMenuAsync(string date)
    {
        var entries = await _menuRepository.GetDailyMenuAsync(date);
        return entries.Select(ToDto).ToList();
    }

    public async Task<DailyMenuEntry> CheckAndReserveQuantityAsync(int dailyMenuId)
    {
        var entry = await _menuRepository.GetDailyMenuEntryByIdAsync(dailyMenuId)
            ?? throw new KeyNotFoundException($"Daily menu entry {dailyMenuId} not found.");

        if (entry.AvailableQuantity <= 0)
            throw new InvalidOperationException("OUT_OF_STOCK");

        entry.AvailableQuantity--;
        return entry;
    }

    public async Task RestoreQuantityAsync(int dailyMenuId)
    {
        var entry = await _menuRepository.GetDailyMenuEntryByIdAsync(dailyMenuId)
            ?? throw new KeyNotFoundException($"Daily menu entry {dailyMenuId} not found.");
        entry.AvailableQuantity++;
    }

    public async Task<ReportOutOfStockResult> ReportOutOfStockAsync(int dailyMenuId)
    {
        var entry = await _menuRepository.GetDailyMenuEntryByIdAsync(dailyMenuId)
            ?? throw new KeyNotFoundException($"Daily menu entry {dailyMenuId} not found.");

        entry.AvailableQuantity = 0;

        var affectedOrders = await _orderRepository.GetActiveOrdersForDailyMenuAsync(dailyMenuId);
        var studentsNotified = new HashSet<int>();

        foreach (var order in affectedOrders)
        {
            var account = await _ledgerRepository.GetAccountByStudentIdAsync(order.StudentId)
                ?? throw new KeyNotFoundException($"No account for student {order.StudentId}.");

            await _ledgerService.RefundBalanceAsync(account, order.AmountCharged, order.Id, "Auto-refund: item reported out of stock");
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            studentsNotified.Add(order.StudentId);
        }

        await _notificationService.PublishAsync(new NotificationEvent("stock_out", new { dailyMenuId }));

        return new ReportOutOfStockResult(ToDto(entry), affectedOrders.Count, studentsNotified.Count);
    }

    private static MenuItemDto ToDto(MenuItem m) => new(m.Id, m.Name, m.Description, m.Price, m.IsActive);

    private static TimeslotDto ToDto(Timeslot t) => new(t.Id, t.Label, t.StartTime, t.EndTime, t.SortOrder);

    private static DailyMenuEntryDto ToDto(DailyMenuEntry d) => new(d.Id, d.Date, d.TimeslotId, d.MenuItemId, d.AvailableQuantity);
}
