namespace LunchTicket.Api.DTOs;

public record PlacePreOrderRequest(int DailyMenuId, int TimeslotId);

public record ConfirmPreOrderRequest(string QrToken);

public record CreateWalkinOrderRequest(string CardToken, int DailyMenuId, int TimeslotId);

public record OrderDto(
    int Id,
    int StudentId,
    int DailyMenuId,
    int TimeslotId,
    string OrderType,
    string Status,
    string? Source,
    decimal AmountCharged,
    string? QrToken,
    DateTime? QrExpiresAt,
    int? PlacedByStaff,
    int? DeliveredBy,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public record CancelPreOrderResponse(OrderDto Order, decimal RefundAmount, decimal RefundPercent);

public record QueueDto(IReadOnlyList<OrderDto> PreOrders, IReadOnlyList<OrderDto> WalkIns);
