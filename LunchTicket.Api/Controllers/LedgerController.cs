using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api/students/{studentId:int}")]
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
        return Ok(await _ledgerService.GetBalanceAsync(studentId));
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(int studentId)
    {
        return Ok(await _ledgerService.GetTransactionsAsync(studentId));
    }

    [HttpPost("topup")]
    public async Task<ActionResult<AccountDto>> TopUp(int studentId, [FromQuery] int actorStaffId, [FromBody] TopUpRequest request)
    {
        return Ok(await _ledgerService.TopUpAsync(studentId, request.Amount, actorStaffId));
    }
}

[ApiController]
[Route("api/reports")]
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
