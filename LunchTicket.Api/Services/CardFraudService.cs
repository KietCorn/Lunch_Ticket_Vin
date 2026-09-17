using System.Security.Cryptography;
using System.Text;
using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;

namespace LunchTicket.Api.Services;

public class CardFraudService : ICardFraudService
{
    private const int FailedScanLockThreshold = 3;

    private readonly ICardRepository _cardRepository;

    public CardFraudService(ICardRepository cardRepository)
    {
        _cardRepository = cardRepository;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        var staff = await _cardRepository.GetStaffByUsernameAsync(username)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!staff.IsActive || staff.PasswordHash != Hash(password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var role = staff.IsAdmin ? "admin" : "staff";
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return new LoginResponse(token, role);
    }

    public async Task<CardDto> IssueCardAsync(int studentId, string cardToken)
    {
        _ = await _cardRepository.GetStudentByIdAsync(studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        // Lost-card flow: lock any existing active card before issuing the new one — balance is untouched by construction.
        var existing = await _cardRepository.GetActiveByStudentIdAsync(studentId);
        if (existing is not null)
        {
            existing.Status = CardStatus.Locked;
            existing.LockedAt = DateTime.UtcNow;
        }

        var card = new Card
        {
            StudentId = studentId,
            CardToken = cardToken,
            Status = CardStatus.Active,
            IssuedAt = DateTime.UtcNow
        };
        await _cardRepository.AddCardAsync(card);

        return ToDto(card);
    }

    public async Task<CardDto> LockCardAsync(int cardId)
    {
        var card = await GetCardOrThrow(cardId);
        card.Status = CardStatus.Locked;
        card.LockedAt = DateTime.UtcNow;
        return ToDto(card);
    }

    public async Task<CardDto> UnlockCardAsync(int cardId)
    {
        var card = await GetCardOrThrow(cardId);
        card.Status = CardStatus.Active;
        card.LockedAt = null;
        card.FailedScanCount = 0;
        return ToDto(card);
    }

    public async Task<CardVerifyResult> VerifyCardAsync(string cardToken)
    {
        var card = await _cardRepository.GetByTokenAsync(cardToken);
        if (card is null)
            return new CardVerifyResult(null, false, false, "CARD_NOT_FOUND");

        if (card.Status == CardStatus.Locked)
            return new CardVerifyResult(ToDto(card), false, true, "CARD_LOCKED");

        return new CardVerifyResult(ToDto(card), true, false, null);
    }

    public async Task<CardVerifyResult> RecordFailedScanAsync(string cardToken)
    {
        var card = await _cardRepository.GetByTokenAsync(cardToken);
        if (card is null)
            return new CardVerifyResult(null, false, false, "CARD_NOT_FOUND");

        card.FailedScanCount++;
        var suspicious = card.FailedScanCount >= FailedScanLockThreshold;
        if (suspicious && card.Status == CardStatus.Active)
        {
            card.Status = CardStatus.Locked;
            card.LockedAt = DateTime.UtcNow;
        }

        return new CardVerifyResult(ToDto(card), false, suspicious, suspicious ? "SUSPICIOUS_USAGE_LOCKED" : "SCAN_FAILED");
    }

    private async Task<Card> GetCardOrThrow(int cardId)
    {
        var card = await _cardRepository.GetByIdAsync(cardId);
        return card ?? throw new KeyNotFoundException($"Card {cardId} not found.");
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static CardDto ToDto(Card c) => new(c.Id, c.StudentId, c.CardToken, c.Status.ToString(), c.IssuedAt, c.LockedAt);
}
