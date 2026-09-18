using LunchTicket.Api.DTOs;

namespace LunchTicket.Api.Services;

public interface ICardFraudService
{
    /// <summary>Tries staff/admin (by username) first, then student (by student code).</summary>
    Task<LoginResponse> LoginAsync(string identifier, string password);
    Task<StudentDto> CreateStudentAsync(string studentCode, string fullName, string? email, string password);
    Task<StaffDto> CreateStaffAsync(string username, string fullName, string password, bool isAdmin);
    Task<CardDto> IssueCardAsync(int studentId, string cardToken);
    Task<CardDto> LockCardAsync(int cardId);
    Task<CardDto> UnlockCardAsync(int cardId);
    Task<CardVerifyResult> VerifyCardAsync(string cardToken);
    Task<CardVerifyResult> RecordFailedScanAsync(string cardToken);
}
