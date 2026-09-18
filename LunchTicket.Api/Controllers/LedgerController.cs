using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:int}")]
[Authorize(Roles = "student,staff,admin")]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _ledgerService;

    public LedgerController(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<AccountDto>> GetBalance(int studentId)
    {
        if (User.IsInRole("student") && studentId != User.GetUserId())
            return Forbid();

        return Ok(await _ledgerService.GetBalanceAsync(studentId));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(int studentId)
    {
        if (User.IsInRole("student") && studentId != User.GetUserId())
            return Forbid();

        return Ok(await _ledgerService.GetTransactionsAsync(studentId));
    }

    [HttpPost("topup")]
    [Authorize(Roles = "staff,admin")]
    public async Task<ActionResult<AccountDto>> TopUp(int studentId, [FromBody] TopUpRequest request)
    {
        return Ok(await _ledgerService.TopUpAsync(studentId, request.Amount, User.GetUserId()));
    }
}

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "staff,admin")]
public class ReportsController : ControllerBase
{
    private readonly ILedgerService _ledgerService;

    public ReportsController(ILedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetAllTransactions([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        return Ok(await _ledgerService.GetAllTransactionsAsync(from, to));
    }
}
