using LunchTicket.Api.Models;

namespace LunchTicket.Api.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<Order?> GetByQrTokenAsync(string qrToken);
    Task<List<Order>> GetByStudentIdAsync(int studentId);
    Task<List<Order>> GetActiveQueueAsync(OrderType orderType);
    Task<bool> HasActiveOrderForSlotAsync(int studentId, int dailyMenuId);
    Task AddAsync(Order order);
    Task<List<Order>> GetActiveOrdersForDailyMenuAsync(int dailyMenuId);
}
