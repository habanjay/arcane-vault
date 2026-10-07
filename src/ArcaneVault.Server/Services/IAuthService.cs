using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IAuthService
{
    UserResponse Register(RegisterRequest request);
    (LoginResponse? Response, TwoFactorChallengeResponse? Challenge) Login(LoginRequest request, string? deviceName);
    LoginResponse Refresh(RefreshRequest request);
    LoginResponse VerifyTwoFactor(TwoFactorVerifyRequest request);
    void Logout(Guid userId, Guid sessionId);
    IReadOnlyList<SessionResponse> GetSessions(Guid userId);
    void RevokeSession(Guid userId, Guid sessionId);
}
