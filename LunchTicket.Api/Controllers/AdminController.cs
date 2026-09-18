using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "admin")]
public class AdminController : ControllerBase
{
    private readonly ICardFraudService _cardFraudService;

    public AdminController(ICardFraudService cardFraudService)
    {
        _cardFraudService = cardFraudService;
    }

    [HttpPost("students")]
    public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentRequest request)
    {
        var student = await _cardFraudService.CreateStudentAsync(request.StudentId, request.FullName, request.Email, request.Password);
        return StatusCode(StatusCodes.Status201Created, student);
    }

    [HttpPost("staff")]
    public async Task<ActionResult<StaffDto>> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var staff = await _cardFraudService.CreateStaffAsync(request.Username, request.FullName, request.Password, request.IsAdmin);
        return StatusCode(StatusCodes.Status201Created, staff);
    }
}
