using LunchTicket.Api.DTOs;

namespace LunchTicket.Api.Services;

public interface ICardFraudService
{
    Task<LoginResponse> LoginAsync(string username, string password);
    Task<CardDto> IssueCardAsync(int studentId, string cardToken);
    Task<CardDto> LockCardAsync(int cardId);
    Task<CardDto> UnlockCardAsync(int cardId);
    Task<CardVerifyResult> VerifyCardAsync(string cardToken);
    Task<CardVerifyResult> RecordFailedScanAsync(string cardToken);
}
