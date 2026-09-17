namespace LunchTicket.Api.DTOs;

public record AccountDto(int Id, int StudentId, decimal Balance, DateTime? UpdatedAt);

public record TransactionDto(
    int Id,
    int AccountId,
    string TransactionType,
    decimal Amount,
    decimal BalanceAfter,
    int? ReferenceOrder,
    int? ActorId,
    string? Note,
    DateTime CreatedAt);

public record TopUpRequest(decimal Amount);
