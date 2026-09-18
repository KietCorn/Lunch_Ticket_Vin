using System.Security.Cryptography;
using System.Text;
using LunchTicket.Api.Data;
using LunchTicket.Api.DTOs;
using LunchTicket.Api.Models;
using LunchTicket.Api.Repositories;

namespace LunchTicket.Api.Services;

public class CardFraudService : ICardFraudService
{
    private const int FailedScanLockThreshold = 3;

    private readonly AppDbContext _db;
    private readonly ICardRepository _cardRepository;
    private readonly ILedgerRepository _ledgerRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public CardFraudService(AppDbContext db, ICardRepository cardRepository, ILedgerRepository ledgerRepository, IJwtTokenService jwtTokenService)
    {
        _db = db;
        _cardRepository = cardRepository;
        _ledgerRepository = ledgerRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LoginResponse> LoginAsync(string identifier, string password)
    {
        var staff = await _cardRepository.GetStaffByUsernameAsync(identifier);
        if (staff is not null)
        {
            if (!staff.IsActive || staff.PasswordHash != Hash(password))
                throw new UnauthorizedAccessException("Invalid credentials.");

            var role = staff.IsAdmin ? "admin" : "staff";
            var token = _jwtTokenService.GenerateToken(staff.Id, role, staff.FullName);
            return new LoginResponse(token, role, staff.Id, staff.FullName);
        }

        var student = await _cardRepository.GetStudentByCodeAsync(identifier)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (student.PasswordHash != Hash(password))
            throw new UnauthorizedAccessException("Invalid credentials.");

        var studentToken = _jwtTokenService.GenerateToken(student.Id, "student", student.FullName);
        return new LoginResponse(studentToken, "student", student.Id, student.FullName);
    }

    public async Task<StudentDto> CreateStudentAsync(string studentCode, string fullName, string? email, string password)
    {
        var student = new Student
        {
            StudentCode = studentCode,
            FullName = fullName,
            Email = email,
            PasswordHash = Hash(password),
            CreatedAt = DateTime.UtcNow
        };
        await _cardRepository.AddStudentAsync(student);

        await _ledgerRepository.AddAccountAsync(new Account
        {
            Student = student,
            Balance = 0
        });

        await _db.SaveChangesAsync();

        return new StudentDto(student.Id, student.StudentCode, student.FullName, student.Email, student.CreatedAt, student.UpdatedAt);
    }

    public async Task<StaffDto> CreateStaffAsync(string username, string fullName, string password, bool isAdmin)
    {
        var staff = new Staff
        {
            Username = username,
            FullName = fullName,
            PasswordHash = Hash(password),
            IsAdmin = isAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        await _cardRepository.AddStaffAsync(staff);
        await _db.SaveChangesAsync();

        return new StaffDto(staff.Id, staff.Username, staff.FullName, staff.IsAdmin, staff.IsActive, staff.CreatedAt);
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
        await _db.SaveChangesAsync();

        return ToDto(card);
    }

    public async Task<CardDto> LockCardAsync(int cardId)
    {
        var card = await GetCardOrThrow(cardId);
        card.Status = CardStatus.Locked;
        card.LockedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return ToDto(card);
    }

    public async Task<CardDto> UnlockCardAsync(int cardId)
    {
        var card = await GetCardOrThrow(cardId);
        card.Status = CardStatus.Active;
        card.LockedAt = null;
        card.FailedScanCount = 0;
        await _db.SaveChangesAsync();
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

        await _db.SaveChangesAsync();

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
