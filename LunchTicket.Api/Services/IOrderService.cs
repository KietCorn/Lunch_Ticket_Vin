using LunchTicket.Api.DTOs;

namespace LunchTicket.Api.Services;

public interface IOrderService
{
    Task<OrderDto> PlacePreOrderAsync(int studentId, int dailyMenuId, int timeslotId);
    Task<CancelPreOrderResponse> CancelPreOrderAsync(int orderId);
    Task<OrderDto> ConfirmPreOrderAsync(string qrToken, int staffId);
    Task<OrderDto> CreateWalkInOrderAsync(string cardToken, int dailyMenuId, int timeslotId, int staffId);
    Task<OrderDto?> GetOrderAsync(int orderId);
    Task<List<OrderDto>> GetOrdersByStudentAsync(int studentId);
    Task<QueueDto> GetQueueAsync();
    Task<OrderDto> MarkReadyAsync(int orderId);
}
