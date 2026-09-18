using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost("pre-orders")]
    [Authorize(Roles = "student")]
    public async Task<ActionResult<OrderDto>> PlacePreOrder([FromBody] PlacePreOrderRequest request)
    {
        var order = await _orderService.PlacePreOrderAsync(User.GetUserId(), request.DailyMenuId, request.TimeslotId);
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpPost("{orderId:int}/cancel")]
    [Authorize(Roles = "student,staff,admin")]
    public async Task<ActionResult<CancelPreOrderResponse>> CancelPreOrder(int orderId)
    {
        if (User.IsInRole("student"))
        {
            var existing = await _orderService.GetOrderAsync(orderId);
            if (existing is null)
                return NotFound();
            if (existing.StudentId != User.GetUserId())
                return Forbid();
        }

        var result = await _orderService.CancelPreOrderAsync(orderId);
        return Ok(result);
    }

    [HttpPost("{orderId:int}/confirm")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<OrderDto>> ConfirmPreOrder(int orderId, [FromBody] ConfirmPreOrderRequest request)
    {
        var order = await _orderService.ConfirmPreOrderAsync(request.QrToken, User.GetUserId());
        return Ok(order);
    }

    [HttpPost("walk-ins")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<OrderDto>> CreateWalkInOrder([FromBody] CreateWalkinOrderRequest request)
    {
        var order = await _orderService.CreateWalkInOrderAsync(request.CardToken, request.DailyMenuId, request.TimeslotId, User.GetUserId());
        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId:int}")]
    [Authorize(Roles = "student,staff,admin")]
    public async Task<ActionResult<OrderDto>> GetOrder(int orderId)
    {
        var order = await _orderService.GetOrderAsync(orderId);
        if (order is null)
            return NotFound();
        if (User.IsInRole("student") && order.StudentId != User.GetUserId())
            return Forbid();
        return Ok(order);
    }

    [HttpGet("queue")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<QueueDto>> GetQueue()
    {
        return Ok(await _orderService.GetQueueAsync());
    }

    [HttpPost("{orderId:int}/ready")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<OrderDto>> MarkReady(int orderId)
    {
        return Ok(await _orderService.MarkReadyAsync(orderId));
    }
}

[ApiController]
[Route("api/students/{studentId:int}/orders")]
[Authorize(Roles = "student,staff,admin")]
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
        if (User.IsInRole("student") && studentId != User.GetUserId())
            return Forbid();

        return Ok(await _orderService.GetOrdersByStudentAsync(studentId));
    }
}
