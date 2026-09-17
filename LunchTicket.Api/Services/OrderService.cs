using LunchTicket.Api.Data;
using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;

namespace LunchTicket.Api.Services;

public class OrderService : IOrderService
{
    private const int CancellationCutoffHours = 2;
    private const decimal FullRefundRate = 1.0m;
    private const decimal LateRefundRate = 0.5m;

    private readonly AppDbContext _db;
    private readonly IOrderRepository _orderRepository;
    private readonly IMenuRepository _menuRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly ILedgerService _ledgerService;
    private readonly IMenuService _menuService;
    private readonly ICardFraudService _cardFraudService;
    private readonly ICardRepository _cardRepository;
    private readonly INotificationService _notificationService;

    public OrderService(
        AppDbContext db,
        IOrderRepository orderRepository,
        IMenuRepository menuRepository,
        ILedgerRepository ledgerRepository,
        ILedgerService ledgerService,
        IMenuService menuService,
        ICardFraudService cardFraudService,
        ICardRepository cardRepository,
        INotificationService notificationService)
    {
        _db = db;
        _orderRepository = orderRepository;
        _menuRepository = menuRepository;
        _ledgerRepository = ledgerRepository;
        _ledgerService = ledgerService;
        _menuService = menuService;
        _cardFraudService = cardFraudService;
        _cardRepository = cardRepository;
        _notificationService = notificationService;
    }

    public async Task<OrderDto> PlacePreOrderAsync(int studentId, int dailyMenuId, int timeslotId)
    {
        if (await _orderRepository.HasActiveOrderForSlotAsync(studentId, dailyMenuId))
            throw new InvalidOperationException("DUPLICATE_ORDER_FOR_SLOT");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var dailyMenu = await _menuService.CheckAndReserveQuantityAsync(dailyMenuId);

        var account = await _ledgerRepository.GetAccountByStudentIdAsync(studentId)
            ?? throw new KeyNotFoundException($"No account for student {studentId}.");

        await _ledgerService.DeductBalanceAsync(account, dailyMenu.MenuItem.Price, null, "Pre-order placed");

        var order = new Order
        {
            StudentId = studentId,
            DailyMenuId = dailyMenuId,
            TimeslotId = timeslotId,
            OrderType = OrderType.PreOrder,
            Status = OrderStatus.Pending,
            AmountCharged = dailyMenu.MenuItem.Price,
            QrToken = $"CARD-{Guid.NewGuid():N}"[..20],
            CreatedAt = DateTime.UtcNow
        };
        await _orderRepository.AddAsync(order);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notificationService.PublishAsync(new NotificationEvent("queue_updated", new { orderId = order.Id }));

        return ToDto(order);
    }

    public async Task<CancelPreOrderResponse> CancelPreOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException("ORDER_NOT_CANCELLABLE");

        var timeslotStart = ParseTimeslotStart(order.DailyMenu.Date, order.Timeslot.StartTime);
        var refundRate = DateTime.UtcNow <= timeslotStart.AddHours(-CancellationCutoffHours) ? FullRefundRate : LateRefundRate;
        var refundAmount = order.AmountCharged * refundRate;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var account = await _ledgerRepository.GetAccountByStudentIdAsync(order.StudentId)
            ?? throw new KeyNotFoundException($"No account for student {order.StudentId}.");

        await _ledgerService.RefundBalanceAsync(account, refundAmount, order.Id, $"Cancellation refund ({refundRate:P0})");
        await _menuService.RestoreQuantityAsync(order.DailyMenuId);

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notificationService.PublishAsync(new NotificationEvent("queue_updated", new { orderId = order.Id }));

        return new CancelPreOrderResponse(ToDto(order), refundAmount, refundRate * 100);
    }

    public async Task<OrderDto> ConfirmPreOrderAsync(string qrToken, int staffId)
    {
        var order = await _orderRepository.GetByQrTokenAsync(qrToken)
            ?? throw new KeyNotFoundException("QR_TOKEN_NOT_FOUND");

        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new InvalidOperationException("QR_ALREADY_USED");

        order.Status = OrderStatus.Delivered;
        order.Source = OrderSource.QrScan;
        order.DeliveredById = staffId;
        order.QrToken = null; // one-time-use: invalidated immediately after first successful scan
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _notificationService.PublishAsync(new NotificationEvent("queue_updated", new { orderId = order.Id }));

        return ToDto(order);
    }

    public async Task<OrderDto> CreateWalkInOrderAsync(string cardToken, int dailyMenuId, int timeslotId, int staffId)
    {
        var verifyResult = await _cardFraudService.VerifyCardAsync(cardToken);
        if (!verifyResult.Valid || verifyResult.Card is null)
            throw new InvalidOperationException(verifyResult.Reason ?? "CARD_INVALID");

        var studentId = verifyResult.Card.StudentId;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var dailyMenu = await _menuService.CheckAndReserveQuantityAsync(dailyMenuId);

        var account = await _ledgerRepository.GetAccountByStudentIdAsync(studentId)
            ?? throw new KeyNotFoundException($"No account for student {studentId}.");

        await _ledgerService.DeductBalanceAsync(account, dailyMenu.MenuItem.Price, null, "Walk-in order");

        var order = new Order
        {
            StudentId = studentId,
            DailyMenuId = dailyMenuId,
            TimeslotId = timeslotId,
            OrderType = OrderType.WalkIn,
            Status = OrderStatus.Ready,
            Source = OrderSource.ManualEntry,
            AmountCharged = dailyMenu.MenuItem.Price,
            PlacedByStaffId = staffId,
            CreatedAt = DateTime.UtcNow
        };
        await _orderRepository.AddAsync(order);

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        await _notificationService.PublishAsync(new NotificationEvent("queue_updated", new { orderId = order.Id }));

        return ToDto(order);
    }

    public async Task<OrderDto?> GetOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order is null ? null : ToDto(order);
    }

    public async Task<List<OrderDto>> GetOrdersByStudentAsync(int studentId)
    {
        var orders = await _orderRepository.GetByStudentIdAsync(studentId);
        return orders.Select(ToDto).ToList();
    }

    public async Task<QueueDto> GetQueueAsync()
    {
        var preOrders = await _orderRepository.GetActiveQueueAsync(OrderType.PreOrder);
        var walkIns = await _orderRepository.GetActiveQueueAsync(OrderType.WalkIn);
        return new QueueDto(preOrders.Select(ToDto).ToList(), walkIns.Select(ToDto).ToList());
    }

    public async Task<OrderDto> MarkReadyAsync(int orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");

        order.Status = OrderStatus.Ready;
        order.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _notificationService.PublishAsync(new NotificationEvent("queue_updated", new { orderId = order.Id }));

        return ToDto(order);
    }

    private static DateTime ParseTimeslotStart(string date, string startTime)
    {
        return DateTime.Parse($"{date}T{startTime}:00");
    }

    private static OrderDto ToDto(Order o) => new(
        o.Id, o.StudentId, o.DailyMenuId, o.TimeslotId, o.OrderType.ToString(), o.Status.ToString(),
        o.Source?.ToString(), o.AmountCharged, o.QrToken, o.QrExpiresAt,
        o.PlacedByStaffId, o.DeliveredById, o.CreatedAt, o.UpdatedAt);
}
