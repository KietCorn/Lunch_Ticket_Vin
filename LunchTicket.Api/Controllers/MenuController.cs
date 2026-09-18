using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/menu")]
[Authorize]
public class MenuController : ControllerBase
{
    private readonly IMenuService _menuService;

    public MenuController(IMenuService menuService)
    {
        _menuService = menuService;
    }

    [HttpGet("items")]
    public async Task<ActionResult<List<MenuItemDto>>> GetMenuItems()
    {
        return Ok(await _menuService.GetMenuItemsAsync());
    }

    [HttpPost("items")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<MenuItemDto>> CreateMenuItem([FromQuery] string name, [FromQuery] string? description, [FromQuery] decimal price)
    {
        return Ok(await _menuService.CreateMenuItemAsync(name, description, price));
    }

    [HttpGet("timeslots")]
    public async Task<ActionResult<List<TimeslotDto>>> GetTimeslots()
    {
        return Ok(await _menuService.GetTimeslotsAsync());
    }

    [HttpGet("daily")]
    public async Task<ActionResult<List<DailyMenuEntryDto>>> GetDailyMenu([FromQuery] string date)
    {
        return Ok(await _menuService.GetDailyMenuAsync(date));
    }

    [HttpPost("daily/{dailyMenuId:int}/report-out-of-stock")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<ReportOutOfStockResult>> ReportOutOfStock(int dailyMenuId)
    {
        return Ok(await _menuService.ReportOutOfStockAsync(dailyMenuId));
    }
}
