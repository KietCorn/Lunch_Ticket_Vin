using LunchTicket.Api.DTOs;
using LunchTicket.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LunchTicket.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "staff,admin")]
public class CardFraudController : ControllerBase
{
    private readonly ICardFraudService _cardFraudService;

    public CardFraudController(ICardFraudService cardFraudService)
    {
        _cardFraudService = cardFraudService;
    }

    [HttpPost("auth/login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            return Ok(await _cardFraudService.LoginAsync(request.Username, request.Password));
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    [HttpPost("students/{studentId:int}/cards")]
    public async Task<ActionResult<CardDto>> IssueCard(int studentId, [FromQuery] string cardToken)
    {
        return Ok(await _cardFraudService.IssueCardAsync(studentId, cardToken));
    }

    [HttpPost("cards/{cardId:int}/lock")]
    public async Task<ActionResult<CardDto>> LockCard(int cardId)
    {
        return Ok(await _cardFraudService.LockCardAsync(cardId));
    }

    [HttpPost("cards/{cardId:int}/unlock")]
    public async Task<ActionResult<CardDto>> UnlockCard(int cardId)
    {
        return Ok(await _cardFraudService.UnlockCardAsync(cardId));
    }

    [HttpPost("cards/{cardToken}/verify")]
    public async Task<ActionResult<CardVerifyResult>> VerifyCard(string cardToken)
    {
        return Ok(await _cardFraudService.VerifyCardAsync(cardToken));
    }

    [HttpPost("cards/{cardToken}/verify/failed")]
    public async Task<ActionResult<CardVerifyResult>> RecordFailedScan(string cardToken)
    {
        return Ok(await _cardFraudService.RecordFailedScanAsync(cardToken));
    }
}
