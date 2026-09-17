using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Repositories;

public class MenuRepository : IMenuRepository
{
    private readonly AppDbContext _db;

    public MenuRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<List<MenuItem>> GetMenuItemsAsync() =>
        _db.MenuItems.ToListAsync();

    public Task<MenuItem?> GetMenuItemByIdAsync(int id) =>
        _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id);

    public async Task AddMenuItemAsync(MenuItem item) => await _db.MenuItems.AddAsync(item);

    public Task<List<Timeslot>> GetTimeslotsAsync() =>
        _db.Timeslots.OrderBy(t => t.SortOrder).ToListAsync();

    public Task<List<DailyMenuEntry>> GetDailyMenuAsync(string date) =>
        _db.DailyMenuEntries.Include(d => d.MenuItem).Include(d => d.Timeslot).Where(d => d.Date == date).ToListAsync();

    public Task<DailyMenuEntry?> GetDailyMenuEntryByIdAsync(int id) =>
        _db.DailyMenuEntries.Include(d => d.MenuItem).Include(d => d.Timeslot).FirstOrDefaultAsync(d => d.Id == id);
}
