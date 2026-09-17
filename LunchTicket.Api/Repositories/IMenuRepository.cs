using LunchTicket.Api.Models;

namespace LunchTicket.Api.Repositories;

public interface IMenuRepository
{
    Task<List<MenuItem>> GetMenuItemsAsync();
    Task<MenuItem?> GetMenuItemByIdAsync(int id);
    Task AddMenuItemAsync(MenuItem item);
    Task<List<Timeslot>> GetTimeslotsAsync();
    Task<List<DailyMenuEntry>> GetDailyMenuAsync(string date);
    Task<DailyMenuEntry?> GetDailyMenuEntryByIdAsync(int id);
}
