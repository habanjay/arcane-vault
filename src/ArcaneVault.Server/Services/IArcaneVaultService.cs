using ArcaneVault.Server.Apis;

namespace ArcaneVault.Server.Services;

public interface IArcaneVaultService
{
    UserResponse Register(RegisterRequest request);
    (LoginResponse? Response, TwoFactorChallengeResponse? Challenge) Login(LoginRequest request, string? deviceName);
    LoginResponse Refresh(RefreshRequest request);
    LoginResponse VerifyTwoFactor(TwoFactorVerifyRequest request);
    void Logout(Guid userId, Guid sessionId);
    IReadOnlyList<SessionResponse> GetSessions(Guid userId);
    void RevokeSession(Guid userId, Guid sessionId);
    UserResponse GetUser(Guid userId);
    UserResponse UpdateUser(Guid userId, UpdateUserRequest request, long expectedVersion);
    SecuritySettingsResponse GetSecuritySettings(Guid userId);
    SecuritySettingsResponse UpdateSecuritySettings(Guid userId, UpdateSecuritySettingsRequest request);
    UserResponse UpdateProfilePhoto(Guid userId, string contentType, long expectedVersion);
    void DeleteProfilePhoto(Guid userId);
    PageResponse<CategoryResponse> GetCategories(Guid userId, int page, int pageSize);
    CategoryResponse GetCategory(Guid userId, Guid id);
    CategoryResponse CreateCategory(Guid userId, CreateCategoryRequest request);
    CategoryResponse UpdateCategory(Guid userId, Guid id, UpdateCategoryRequest request);
    void DeleteCategory(Guid userId, Guid id);
    PageResponse<TagResponse> GetTags(Guid userId, int page, int pageSize);
    TagResponse CreateTag(Guid userId, CreateTagRequest request);
    void DeleteTag(Guid userId, Guid id);
    PageResponse<VaultItemResponse> GetVaultItems(Guid userId, Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int page, int pageSize);
    VaultItemDetailResponse GetVaultItem(Guid userId, Guid id);
    VaultItemDetailResponse CreateVaultItem(Guid userId, CreateVaultItemRequest request);
    VaultItemDetailResponse UpdateVaultItem(Guid userId, Guid id, UpdateVaultItemRequest request, long expectedVersion);
    void DeleteVaultItem(Guid userId, Guid id);
    VaultItemResponse ToggleFavorite(Guid userId, Guid id, long expectedVersion);
    RevealVaultItemResponse RevealVaultItem(Guid userId, Guid id, string masterPassword);
    IReadOnlyList<PasswordHistoryResponse> GetPasswordHistory(Guid userId, Guid id);
    ShareResponse CreateShare(Guid userId, Guid id, CreateShareRequest request);
    IReadOnlyList<ShareResponse> GetShares(Guid userId, Guid id);
    void DeleteShare(Guid userId, Guid id, Guid shareId);
    PageResponse<AuditLogResponse> GetAuditLogs(Guid userId, string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize);
    AuditSummaryResponse GetAuditSummary(Guid userId);
    DashboardSummaryResponse GetDashboardSummary(Guid userId);
}

public sealed class ApiException(int statusCode, string code, string message, IReadOnlyList<ApiErrorDetail>? details = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public IReadOnlyList<ApiErrorDetail>? Details { get; } = details;
}
