using LunchTicket.Api.Services;

namespace LunchTicket.Api.Tests;

/// <summary>Avoids wiring up real IConfiguration in tests that never inspect the token itself.</summary>
public class FakeJwtTokenService : IJwtTokenService
{
    public string GenerateToken(int id, string role, string displayName) => $"fake-token-{id}-{role}";
}
