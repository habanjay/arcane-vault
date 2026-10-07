using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace ArcaneVault.Server.Mocks;

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
