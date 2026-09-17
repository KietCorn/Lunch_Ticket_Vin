namespace LunchTicket.Api.DTOs;

public record NotificationEvent(string Type, object? Payload);
