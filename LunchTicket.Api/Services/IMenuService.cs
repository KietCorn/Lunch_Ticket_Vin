using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;

namespace LunchTicket.Api.Services;

public interface IMenuService
{
    Task<List<MenuItemDto>> GetMenuItemsAsync();
    Task<MenuItemDto> CreateMenuItemAsync(string name, string? description, decimal price);
    Task<List<TimeslotDto>> GetTimeslotsAsync();
    Task<List<DailyMenuEntryDto>> GetDailyMenuAsync(string date);

    /// <summary>Throws InvalidOperationException if the slot has no quantity left. Caller controls the SaveChanges boundary.</summary>
    Task<DailyMenuEntry> CheckAndReserveQuantityAsync(int dailyMenuId);
    Task RestoreQuantityAsync(int dailyMenuId);
    Task<ReportOutOfStockResult> ReportOutOfStockAsync(int dailyMenuId);
}
