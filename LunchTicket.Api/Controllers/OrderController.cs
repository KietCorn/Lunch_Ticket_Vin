using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    // studentId/staffId are accepted as query params until student/staff auth (JWT claims) is designed — see CLAUDE.md "Known Gaps".

    [HttpPost("pre-orders")]
    public async Task<ActionResult<OrderDto>> PlacePreOrder([FromQuery] int studentId, [FromBody] PlacePreOrderRequest request)
    {
        var order = await _orderService.PlacePreOrderAsync(studentId, request.DailyMenuId, request.TimeslotId);
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpPost("{orderId:int}/cancel")]
    public async Task<ActionResult<CancelPreOrderResponse>> CancelPreOrder(int orderId)
    {
        var result = await _orderService.CancelPreOrderAsync(orderId);
        return Ok(result);
    }

    [HttpPost("{orderId:int}/confirm")]
    public async Task<ActionResult<OrderDto>> ConfirmPreOrder(int orderId, [FromQuery] int staffId, [FromBody] ConfirmPreOrderRequest request)
    {
        var order = await _orderService.ConfirmPreOrderAsync(request.QrToken, staffId);
        return Ok(order);
    }

    [HttpPost("walk-ins")]
    public async Task<ActionResult<OrderDto>> CreateWalkInOrder([FromQuery] int staffId, [FromBody] CreateWalkinOrderRequest request)
    {
        var order = await _orderService.CreateWalkInOrderAsync(request.CardToken, request.DailyMenuId, request.TimeslotId, staffId);
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId:int}")]
    public async Task<ActionResult<OrderDto>> GetOrder(int orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("queue")]
    public async Task<ActionResult<QueueDto>> GetQueue()
    {
        return Ok(await _orderService.GetQueueAsync());
    }

    [HttpPost("{orderId:int}/ready")]
    public async Task<ActionResult<OrderDto>> MarkReady(int orderId)
    {
        return Ok(await _orderService.MarkReadyAsync(orderId));
    }
}

[ApiController]
[Route("api/students/{studentId:int}/orders")]
public class StudentOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public StudentOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderDto>>> GetOrdersByStudent(int studentId)
    {
        return Ok(await _orderService.GetOrdersByStudentAsync(studentId));
    }
}
