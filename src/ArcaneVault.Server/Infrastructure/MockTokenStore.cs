using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using ArcaneVault.Server.Apis;

namespace ArcaneVault.Server.Infrastructure;

public sealed class MockTokenStore
{
    private readonly ConcurrentDictionary<string, TokenRecord> _accessTokens = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TokenRecord> _refreshTokens = new(StringComparer.Ordinal);

    public (string AccessToken, string RefreshToken) Issue(Guid userId, Guid sessionId)
    {
        var accessToken = CreateToken();
        var refreshToken = CreateToken();
        var record = new TokenRecord(userId, sessionId, DateTimeOffset.UtcNow.AddMinutes(15));
        _accessTokens[accessToken] = record;
        _refreshTokens[refreshToken] = record with { ExpiresAt = DateTimeOffset.UtcNow.AddDays(30) };
        return (accessToken, refreshToken);
    }

    public bool TryGetAccessToken(string token, out Guid userId, out Guid sessionId)
    {
        if (_accessTokens.TryGetValue(token, out var record) && record.ExpiresAt > DateTimeOffset.UtcNow)
        {
            userId = record.UserId;
            sessionId = record.SessionId;
            return true;
        }

        userId = Guid.Empty;
        sessionId = Guid.Empty;
        return false;
    }

    public bool TryRotateRefreshToken(string token, out Guid userId, out Guid sessionId)
    {
        if (_refreshTokens.TryRemove(token, out var record) && record.ExpiresAt > DateTimeOffset.UtcNow)
        {
            userId = record.UserId;
            sessionId = record.SessionId;
            return true;
        }

        userId = Guid.Empty;
        sessionId = Guid.Empty;
        return false;
    }

    public void RevokeSession(Guid userId, Guid sessionId)
    {
        RemoveTokens(_accessTokens, userId, sessionId);
        RemoveTokens(_refreshTokens, userId, sessionId);
    }

    private static void RemoveTokens(ConcurrentDictionary<string, TokenRecord> tokens, Guid userId, Guid sessionId)
    {
        foreach (var entry in tokens)
        {
            if (entry.Value.UserId == userId && entry.Value.SessionId == sessionId)
            {
                tokens.TryRemove(entry.Key, out _);
            }
        }
    }

    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed record TokenRecord(Guid UserId, Guid SessionId, DateTimeOffset ExpiresAt);
}

public sealed class MockBearerHandler(
    Microsoft.Extensions.Options.IOptionsMonitor<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    System.Text.Encodings.Web.UrlEncoder encoder,
    MockTokenStore tokens) : Microsoft.AspNetCore.Authentication.AuthenticationHandler<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<Microsoft.AspNetCore.Authentication.AuthenticateResult> HandleAuthenticateAsync()
    {
        var authorization = Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.NoResult());
        }

        var token = authorization["Bearer ".Length..].Trim();
        if (!tokens.TryGetAccessToken(token, out var userId, out var sessionId))
        {
            return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Fail("The access token is invalid or expired."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim("session_id", sessionId.ToString())
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        return Task.FromResult(Microsoft.AspNetCore.Authentication.AuthenticateResult.Success(
            new Microsoft.AspNetCore.Authentication.AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(Microsoft.AspNetCore.Authentication.AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Response.WriteAsJsonAsync(new ApiErrorResponse(new ApiError("UNAUTHORIZED", "A valid bearer access token is required.")));
    }
}
