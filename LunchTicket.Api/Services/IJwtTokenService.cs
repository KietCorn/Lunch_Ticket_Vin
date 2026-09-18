namespace LunchTicket.Api.Services;

public interface IJwtTokenService
{
    string GenerateToken(int id, string role, string displayName);
}
