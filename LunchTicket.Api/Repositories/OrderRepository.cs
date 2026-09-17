using LunchTicket.Api.Data;
using LunchTicket.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LunchTicket.Api.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;

    public OrderRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<Order?> GetByIdAsync(int id) =>
        _db.Orders.Include(o => o.DailyMenu).Include(o => o.Timeslot).FirstOrDefaultAsync(o => o.Id == id);

    public Task<Order?> GetByQrTokenAsync(string qrToken) =>
        _db.Orders.FirstOrDefaultAsync(o => o.QrToken == qrToken);

    public Task<List<Order>> GetByStudentIdAsync(int studentId) =>
        _db.Orders.Where(o => o.StudentId == studentId).OrderByDescending(o => o.CreatedAt).ToListAsync();

    public Task<List<Order>> GetActiveQueueAsync(OrderType orderType) =>
        _db.Orders
            .Where(o => o.OrderType == orderType && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Ready))
            .OrderBy(o => o.CreatedAt)
            .ToListAsync();

    public Task<bool> HasActiveOrderForSlotAsync(int studentId, int dailyMenuId) =>
        _db.Orders.AnyAsync(o => o.StudentId == studentId && o.DailyMenuId == dailyMenuId && o.Status != OrderStatus.Cancelled);

    public async Task AddAsync(Order order) => await _db.Orders.AddAsync(order);

    public Task<List<Order>> GetActiveOrdersForDailyMenuAsync(int dailyMenuId) =>
        _db.Orders
            .Where(o => o.DailyMenuId == dailyMenuId && (o.Status == OrderStatus.Pending || o.Status == OrderStatus.Ready))
            .ToListAsync();
}
