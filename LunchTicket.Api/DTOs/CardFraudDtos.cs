namespace LunchTicket.Api.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string Role, int Id, string FullName);

public record CardDto(int Id, int StudentId, string CardToken, string Status, DateTime IssuedAt, DateTime? LockedAt);

public record CardVerifyResult(CardDto? Card, bool Valid, bool Suspicious, string? Reason);
