using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArcaneVault.Server.Dtos;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;

namespace ArcaneVault.Server.Services;

public sealed class SqlArcaneVaultService : IAuthService, IAccessTokenValidator, IUserService, ICategoryService,
    ITagService, IVaultItemService, IAuditService, IDashboardService
{
    private const int PasswordIterations = 600_000;
    private const int TokenLifetimeSeconds = 900;
    private readonly string _connectionString;
    private readonly IDataProtector _secretProtector;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public SqlArcaneVaultService(IConfiguration configuration, IDataProtectionProvider dataProtectionProvider)
    {
        _connectionString = configuration.GetConnectionString("ArcaneVault")
            ?? throw new InvalidOperationException("Connection string 'ArcaneVault' is required.");
        _secretProtector = dataProtectionProvider.CreateProtector("ArcaneVault.VaultSecret.v1");
    }

    public UserResponse Register(RegisterRequest request)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var passwordHash = HashPassword(request.MasterPassword, salt);
        return Call<UserResponse>("Auth_Execute", "Register", new
        {
            Email = NormalizeEmail(request.Email),
            request.FirstName,
            request.LastName,
            PasswordSalt = Convert.ToBase64String(salt),
            PasswordHash = Convert.ToBase64String(passwordHash)
        });
    }

    public (LoginResponse? Response, TwoFactorChallengeResponse? Challenge) Login(LoginRequest request, string? deviceName)
    {
        var user = Call<LoginUserRecord?>("Auth_Execute", "FindForLogin", new { Email = NormalizeEmail(request.Email) });
        if (user is null || !VerifyPassword(request.MasterPassword, user.PasswordSalt, user.PasswordHash))
        {
            Call<object>("Auth_Execute", "RecordFailedLogin", new { Email = NormalizeEmail(request.Email) });
            throw Unauthorized("INVALID_CREDENTIALS", "The email or master password is incorrect.");
        }

        if (user.TwoFactorEnabled)
        {
            throw new ApiException(StatusCodes.Status422UnprocessableEntity, "TWO_FACTOR_ENROLLMENT_REQUIRED",
                "Two-factor authentication cannot be completed until an enrollment flow is configured.");
        }

        return (CreateSession(user.Id, user.Email, user.FirstName, user.LastName, user.Role, deviceName), null);
    }

    public LoginResponse Refresh(RefreshRequest request)
    {
        var tokens = CreateTokens();
        var state = Call<LoginSessionRecord?>("Auth_Execute", "RotateRefreshToken", new
        {
            RefreshTokenHash = HashToken(request.RefreshToken),
            AccessTokenHash = HashToken(tokens.AccessToken),
            NewRefreshTokenHash = HashToken(tokens.RefreshToken)
        });
        if (state is null) throw Unauthorized("INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");
        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, TokenLifetimeSeconds,
            new UserSummaryResponse(state.UserId, state.Email, state.FirstName, state.LastName, state.Role));
    }

    public LoginResponse VerifyTwoFactor(TwoFactorVerifyRequest request)
    {
        var user = Call<LoginUserRecord?>("Auth_Execute", "VerifyTwoFactor", new
        {
            request.ChallengeId,
            request.Code
        });
        if (user is null) throw Unauthorized("INVALID_2FA_CODE", "The verification code is invalid.");
        return CreateSession(user.Id, user.Email, user.FirstName, user.LastName, user.Role, "Verified device");
    }

    public void Logout(Guid userId, Guid sessionId) =>
        Call<object>("Auth_Execute", "RevokeSession", new { UserId = userId, SessionId = sessionId });

    public IReadOnlyList<SessionResponse> GetSessions(Guid userId) =>
        Call<IReadOnlyList<SessionResponse>>("Auth_Execute", "GetSessions", new { UserId = userId });

    public void RevokeSession(Guid userId, Guid sessionId) =>
        Call<object>("Auth_Execute", "RevokeSession", new { UserId = userId, SessionId = sessionId });

    public bool TryValidateAccessToken(string token, out Guid userId, out Guid sessionId)
    {
        var state = Call<AccessTokenRecord?>("Auth_Execute", "ValidateAccessToken", new { AccessTokenHash = HashToken(token) });
        userId = state?.UserId ?? Guid.Empty;
        sessionId = state?.SessionId ?? Guid.Empty;
        return state is not null;
    }

    public UserResponse GetUser(Guid userId) =>
        Call<UserResponse>("Users_Execute", "Get", new { UserId = userId });

    public UserResponse UpdateUser(Guid userId, UpdateUserRequest request, long expectedVersion) =>
        Call<UserResponse>("Users_Execute", "Update", new { UserId = userId, ExpectedVersion = expectedVersion, request.Email, request.FirstName, request.LastName });

    public SecuritySettingsResponse GetSecuritySettings(Guid userId) =>
        Call<SecuritySettingsResponse>("Users_Execute", "GetSecuritySettings", new { UserId = userId });

    public SecuritySettingsResponse UpdateSecuritySettings(Guid userId, UpdateSecuritySettingsRequest request) =>
        Call<SecuritySettingsResponse>("Users_Execute", "UpdateSecuritySettings", new
        {
            UserId = userId,
            request.TwoFactorEnabled,
            request.AutoLockEnabled,
            request.AutoLockMinutes,
            request.Theme,
            request.SecurityRemindersEnabled
        });

    public UserResponse UpdateProfilePhoto(Guid userId, string contentType, byte[] photo, long expectedVersion) =>
        Call<UserResponse>("Users_Execute", "UpdateProfilePhoto", new
        {
            UserId = userId,
            ContentType = contentType,
            Content = Convert.ToBase64String(photo),
            ExpectedVersion = expectedVersion
        });

    public UserProfilePhotoResponse? GetProfilePhoto(Guid userId)
    {
        var photo = Call<StoredProfilePhoto?>("Users_Execute", "GetProfilePhoto", new { UserId = userId });
        return photo is null ? null : new UserProfilePhotoResponse(Convert.FromBase64String(photo.Content), photo.ContentType);
    }

    public void DeleteProfilePhoto(Guid userId) =>
        Call<object>("Users_Execute", "DeleteProfilePhoto", new { UserId = userId });

    public PageResponse<CategoryResponse> GetCategories(Guid userId, int page, int pageSize) =>
        Call<PageResponse<CategoryResponse>>("Categories_Execute", "List", new { UserId = userId, Page = page, PageSize = pageSize });

    public CategoryResponse GetCategory(Guid userId, Guid id) =>
        Call<CategoryResponse>("Categories_Execute", "Get", new { UserId = userId, Id = id });

    public CategoryResponse CreateCategory(Guid userId, CreateCategoryRequest request) =>
        Call<CategoryResponse>("Categories_Execute", "Create", new { UserId = userId, request.Name, request.Description, request.Icon, request.Color });

    public CategoryResponse UpdateCategory(Guid userId, Guid id, UpdateCategoryRequest request) =>
        Call<CategoryResponse>("Categories_Execute", "Update", new { UserId = userId, Id = id, request.Name, request.Description, request.Icon, request.Color });

    public void DeleteCategory(Guid userId, Guid id) =>
        Call<object>("Categories_Execute", "Delete", new { UserId = userId, Id = id });

    public PageResponse<TagResponse> GetTags(Guid userId, int page, int pageSize) =>
        Call<PageResponse<TagResponse>>("Tags_Execute", "List", new { UserId = userId, Page = page, PageSize = pageSize });

    public TagResponse CreateTag(Guid userId, CreateTagRequest request) =>
        Call<TagResponse>("Tags_Execute", "Create", new { UserId = userId, request.Name });

    public void DeleteTag(Guid userId, Guid id) =>
        Call<object>("Tags_Execute", "Delete", new { UserId = userId, Id = id });

    public PageResponse<VaultItemResponse> GetVaultItems(Guid userId, Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int page, int pageSize) =>
        Call<PageResponse<VaultItemResponse>>("VaultItems_Execute", "List", new
        {
            UserId = userId,
            CategoryId = category,
            TagId = tag,
            IsFavorite = favorite,
            Search = search,
            Sort = sort,
            Page = page,
            PageSize = pageSize
        });

    public VaultItemDetailResponse GetVaultItem(Guid userId, Guid id) =>
        ToVaultItemDetail(Call<VaultDetailRecord>("VaultItems_Execute", "Get", new { UserId = userId, Id = id }));

    public VaultItemDetailResponse CreateVaultItem(Guid userId, CreateVaultItemRequest request)
    {
        var result = Call<VaultDetailRecord>("VaultItems_Execute", "Create", new
        {
            UserId = userId,
            request.ServiceName,
            request.SiteUrl,
            request.Username,
            EncryptedPassword = Protect(request.Password),
            PasswordStrengthScore = StrengthScore(request.Password),
            request.CategoryId,
            request.TagIds,
            EncryptedNotes = request.Notes is null ? null : Protect(request.Notes)
        });
        return ToVaultItemDetail(result);
    }

    public VaultItemDetailResponse UpdateVaultItem(Guid userId, Guid id, UpdateVaultItemRequest request, long expectedVersion)
    {
        var result = Call<VaultDetailRecord>("VaultItems_Execute", "Update", new
        {
            UserId = userId,
            Id = id,
            ExpectedVersion = expectedVersion,
            request.ServiceName,
            request.SiteUrl,
            request.Username,
            EncryptedPassword = request.Password is null ? null : Protect(request.Password),
            PasswordStrengthScore = request.Password is null ? (int?)null : StrengthScore(request.Password),
            request.CategoryId,
            request.TagIds,
            EncryptedNotes = request.Notes is null ? null : Protect(request.Notes),
            request.IsFavorite
        });
        return ToVaultItemDetail(result);
    }

    public void DeleteVaultItem(Guid userId, Guid id) =>
        Call<object>("VaultItems_Execute", "Delete", new { UserId = userId, Id = id });

    public VaultItemResponse ToggleFavorite(Guid userId, Guid id, long expectedVersion) =>
        Call<VaultItemResponse>("VaultItems_Execute", "ToggleFavorite", new { UserId = userId, Id = id, ExpectedVersion = expectedVersion });

    public RevealVaultItemResponse RevealVaultItem(Guid userId, Guid id, string masterPassword)
    {
        var verifier = Call<LoginUserRecord?>("Auth_Execute", "GetPasswordVerifier", new { UserId = userId });
        if (verifier is null || !VerifyPassword(masterPassword, verifier.PasswordSalt, verifier.PasswordHash))
            throw Unauthorized("INVALID_CREDENTIALS", "The master password is incorrect.");

        var result = Call<ProtectedVaultSecret>("VaultItems_Execute", "Reveal", new { UserId = userId, Id = id });
        var password = Encoding.UTF8.GetString(_secretProtector.Unprotect(Convert.FromBase64String(result.EncryptedPassword)));
        return new RevealVaultItemResponse(password, result.RevealedAt);
    }

    public IReadOnlyList<PasswordHistoryResponse> GetPasswordHistory(Guid userId, Guid id) =>
        Call<IReadOnlyList<PasswordHistoryResponse>>("VaultItems_Execute", "GetPasswordHistory", new { UserId = userId, Id = id });

    public ShareResponse CreateShare(Guid userId, Guid id, CreateShareRequest request) =>
        Call<ShareResponse>("VaultItems_Execute", "CreateShare", new { UserId = userId, Id = id, request.SharedWithEmail, request.Permission });

    public IReadOnlyList<ShareResponse> GetShares(Guid userId, Guid id) =>
        Call<IReadOnlyList<ShareResponse>>("VaultItems_Execute", "GetShares", new { UserId = userId, Id = id });

    public void DeleteShare(Guid userId, Guid id, Guid shareId) =>
        Call<object>("VaultItems_Execute", "DeleteShare", new { UserId = userId, Id = id, ShareId = shareId });

    public PageResponse<AuditLogResponse> GetAuditLogs(Guid userId, string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize) =>
        Call<PageResponse<AuditLogResponse>>("Audit_Execute", "List", new { UserId = userId, Severity = severity, EventType = eventType, From = from, To = to, Page = page, PageSize = pageSize });

    public AuditSummaryResponse GetAuditSummary(Guid userId) =>
        Call<AuditSummaryResponse>("Audit_Execute", "GetSummary", new { UserId = userId });

    public DashboardSummaryResponse GetDashboardSummary(Guid userId) =>
        Call<DashboardSummaryResponse>("Dashboard_Execute", "GetSummary", new { UserId = userId });

    private LoginResponse CreateSession(Guid userId, string email, string firstName, string lastName, string role, string? deviceName)
    {
        var tokens = CreateTokens();
        Call<object>("Auth_Execute", "CreateSession", new
        {
            UserId = userId,
            DeviceName = deviceName,
            AccessTokenHash = HashToken(tokens.AccessToken),
            RefreshTokenHash = HashToken(tokens.RefreshToken)
        });
        return new LoginResponse(tokens.AccessToken, tokens.RefreshToken, TokenLifetimeSeconds,
            new UserSummaryResponse(userId, email, firstName, lastName, role));
    }

    private T Call<T>(string procedure, string action, object payload)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(procedure, connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.Add("@Action", SqlDbType.VarChar, 40).Value = action;
            command.Parameters.Add("@Payload", SqlDbType.NVarChar, -1).Value = JsonSerializer.Serialize(payload, JsonOptions);
            connection.Open();
            var result = command.ExecuteScalar();
            var json = result is null or DBNull ? "null" : (string)result;
            return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
        }
        catch (SqlException exception) when (TryMapDatabaseError(exception.Number, out _))
        {
            TryMapDatabaseError(exception.Number, out var mapped);
            throw mapped;
        }
    }

    private static bool TryMapDatabaseError(int number, out ApiException mapped)
    {
        mapped = number switch
        {
            2627 or 2601 => new ApiException(StatusCodes.Status409Conflict, "RESOURCE_ALREADY_EXISTS", "A resource with these details already exists."),
            50016 => new ApiException(StatusCodes.Status409Conflict, "EMAIL_ALREADY_EXISTS", "An account with that email already exists."),
            50020 => new ApiException(StatusCodes.Status409Conflict, "CATEGORY_NAME_EXISTS", "A category with that name already exists."),
            50021 => new ApiException(StatusCodes.Status409Conflict, "TAG_NAME_EXISTS", "A tag with that name already exists."),
            50022 => new ApiException(StatusCodes.Status409Conflict, "SHARE_ALREADY_EXISTS", "An active share already exists for this user."),
            50002 => Unauthorized("INVALID_2FA_CODE", "The verification code is invalid."),
            50003 or 50004 or 50007 or 50008 or 50012 or 50014 =>
                new ApiException(StatusCodes.Status404NotFound, "RESOURCE_NOT_FOUND", "The requested resource was not found."),
            50005 => new ApiException(StatusCodes.Status409Conflict, "STALE_VERSION", "The resource has changed. Fetch the latest version and retry."),
            50006 or 50009 or 50010 or 50011 => new ApiException(StatusCodes.Status400BadRequest, "INVALID_REQUEST", "The request contains invalid values."),
            50013 => new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_SHARE_PERMISSION", "permission must be View or Edit.",
                [new ApiErrorDetail("permission", "must be View or Edit")]),
            50017 => new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_CATEGORY_COLOR",
                "color must be pink, blue, green, orange, or purple.",
                [new ApiErrorDetail("color", "must be pink, blue, green, orange, or purple")]),
            50018 => new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_AUTO_LOCK_MINUTES",
                "autoLockMinutes must be between 1 and 120.",
                [new ApiErrorDetail("autoLockMinutes", "must be between 1 and 120")]),
            50019 => new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_THEME",
                "theme must be Light, Dark, or System.",
                [new ApiErrorDetail("theme", "must be Light, Dark, or System")]),
            50015 => new ApiException(StatusCodes.Status422UnprocessableEntity, "TWO_FACTOR_ENROLLMENT_REQUIRED",
                "Two-factor authentication cannot be enabled until an enrollment flow is configured."),
            _ => null!
        };
        return mapped is not null;
    }

    private string Protect(string secret) =>
        Convert.ToBase64String(_secretProtector.Protect(Encoding.UTF8.GetBytes(secret)));

    private VaultItemDetailResponse ToVaultItemDetail(VaultDetailRecord item) =>
        new(item.Id, item.ServiceName, item.SiteUrl, item.Username,
            item.EncryptedNotes is null ? null : Encoding.UTF8.GetString(_secretProtector.Unprotect(Convert.FromBase64String(item.EncryptedNotes))),
            item.Category, item.Tags, item.IconInitial, item.ColorTone, item.PasswordStrengthScore, item.IsFavorite,
            item.LastUsedAt, item.CreatedAt, item.UpdatedAt, item.RowVersion);

    private static LoginTokens CreateTokens() => new(CreateToken(), CreateToken());
    private static string CreateToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static string HashToken(string token) => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);
    private static int StrengthScore(string password)
    {
        var classes = (password.Any(char.IsLower) ? 1 : 0) + (password.Any(char.IsUpper) ? 1 : 0)
            + (password.Any(char.IsDigit) ? 1 : 0) + (password.Any(character => !char.IsLetterOrDigit(character)) ? 1 : 0);
        return Math.Clamp((password.Length >= 12 ? 1 : 0) + (password.Length >= 16 ? 1 : 0) + Math.Max(0, classes - 2), 0, 4);
    }
    private static bool VerifyPassword(string password, string salt, string expected) =>
        CryptographicOperations.FixedTimeEquals(HashPassword(password, Convert.FromBase64String(salt)), Convert.FromBase64String(expected));
    private static ApiException Unauthorized(string code, string message) =>
        new(StatusCodes.Status401Unauthorized, code, message);

    private sealed record LoginTokens(string AccessToken, string RefreshToken);
    private sealed record LoginUserRecord(Guid Id, string Email, string FirstName, string LastName, string Role,
        string PasswordSalt, string PasswordHash, bool TwoFactorEnabled);
    private sealed record LoginSessionRecord(Guid UserId, string Email, string FirstName, string LastName, string Role);
    private sealed record AccessTokenRecord(Guid UserId, Guid SessionId);
    private sealed record ProtectedVaultSecret(string EncryptedPassword, DateTimeOffset RevealedAt);
    private sealed record VaultDetailRecord(Guid Id, string ServiceName, string? SiteUrl, string Username, string? EncryptedNotes,
        CategorySummaryResponse? Category, IReadOnlyList<TagSummaryResponse> Tags, string IconInitial, string ColorTone,
        int PasswordStrengthScore, bool IsFavorite, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt, long RowVersion);
    private sealed record StoredProfilePhoto(string Content, string ContentType);
}
