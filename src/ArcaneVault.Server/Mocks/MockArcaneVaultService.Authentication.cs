using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public UserResponse Register(RegisterRequest request)
    {
        lock (_gate)
        {
            var email = NormalizeEmail(request.Email);
            if (_users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                throw Conflict("EMAIL_ALREADY_EXISTS", "An account with that email already exists.");
            }

            var user = CreateUser(email, request.FirstName.Trim(), request.LastName.Trim(), request.MasterPassword);
            _users.Add(user);
            return ToUserResponse(user);
        }
    }
    public (LoginResponse? Response, TwoFactorChallengeResponse? Challenge) Login(LoginRequest request, string? deviceName)
    {
        lock (_gate)
        {
            var email = NormalizeEmail(request.Email);
            var user = _users.FirstOrDefault(candidate => candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user is null || !VerifyPassword(request.MasterPassword, user.PasswordSalt, user.PasswordHash))
            {
                AddAudit(user?.Id, "FailedSignIn", "Unknown device", "Critical");
                throw Unauthorized("INVALID_CREDENTIALS", "The email or master password is incorrect.");
            }

            if (user.Security.TwoFactorEnabled)
            {
                var challengeId = Guid.NewGuid();
                _twoFactorChallenges[challengeId] = user.Id;
                return (null, new TwoFactorChallengeResponse(true, challengeId));
            }

            user.LastLoginAt = DateTimeOffset.UtcNow;
            var session = CreateSession(user.Id, deviceName);
            AddAudit(user.Id, "NewSignIn", session.DeviceName ?? "Unknown device", "Review");
            return (CreateLoginResponse(user, session), null);
        }
    }
    public LoginResponse Refresh(RefreshRequest request)
    {
        lock (_gate)
        {
            if (!tokens.TryRotateRefreshToken(request.RefreshToken, out var userId, out var sessionId))
            {
                throw Unauthorized("INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");
            }

            var session = FindSession(userId, sessionId);
            var user = FindUser(userId);
            session.LastActiveAt = DateTimeOffset.UtcNow;
            return CreateLoginResponse(user, session);
        }
    }
    public LoginResponse VerifyTwoFactor(TwoFactorVerifyRequest request)
    {
        lock (_gate)
        {
            if (!_twoFactorChallenges.Remove(request.ChallengeId, out var userId))
            {
                throw Unauthorized("INVALID_2FA_CODE", "The verification code is invalid.");
            }

            if (request.Code != "123456")
            {
                AddAudit(userId, "FailedSignIn", "Two-factor verification failed", "Critical");
                throw Unauthorized("INVALID_2FA_CODE", "The verification code is invalid.");
            }

            var user = FindUser(userId);
            user.LastLoginAt = DateTimeOffset.UtcNow;
            var session = CreateSession(userId, "Verified device");
            AddAudit(userId, "NewSignIn", session.DeviceName!, "Review");
            return CreateLoginResponse(user, session);
        }
    }
    public void Logout(Guid userId, Guid sessionId)
    {
        lock (_gate)
        {
            RevokeSessionCore(userId, sessionId);
        }
    }
    public IReadOnlyList<SessionResponse> GetSessions(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            return _sessions.Where(session => session.UserId == userId && !session.IsRevoked && session.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(session => session.LastActiveAt)
                .Select(ToSessionResponse)
                .ToArray();
        }
    }
    public void RevokeSession(Guid userId, Guid sessionId)
    {
        lock (_gate)
        {
            RevokeSessionCore(userId, sessionId);
        }
    }
}
