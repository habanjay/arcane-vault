using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Apis;

/// <summary>Represents a paginated collection.</summary>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

/// <summary>Describes a validation issue in an API error response.</summary>
public sealed record ApiErrorDetail(string Field, string Issue);

/// <summary>Provides the documented error response envelope.</summary>
public sealed record ApiErrorResponse(ApiError Error);

/// <summary>Describes an API error.</summary>
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiErrorDetail>? Details = null);

/// <summary>Represents a user profile.</summary>
public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role, string? ProfilePhotoUrl, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt, long RowVersion);

/// <summary>Provides the fields required to register an account.</summary>
public sealed record RegisterRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [Required, MinLength(12), MaxLength(256)]
    public required string MasterPassword { get; init; }

    [Required, MaxLength(50)]
    public required string FirstName { get; init; }

    [Required, MaxLength(50)]
    public required string LastName { get; init; }
}

/// <summary>Provides the credentials required to sign in.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public required string Email { get; init; }

    [Required, MaxLength(256)]
    public required string MasterPassword { get; init; }

    [MaxLength(100)]
    public string? DeviceName { get; init; }
}

/// <summary>Contains the tokens and user returned after sign-in.</summary>
public sealed record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserSummaryResponse User);

/// <summary>Represents the pending second-factor challenge.</summary>
public sealed record TwoFactorChallengeResponse(bool TwoFactorRequired, Guid ChallengeId);

/// <summary>Provides a refresh token.</summary>
public sealed record RefreshRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}

/// <summary>Provides the information required to complete a two-factor challenge.</summary>
public sealed record TwoFactorVerifyRequest
{
    [Required]
    public required Guid ChallengeId { get; init; }

    [Required, MinLength(6), MaxLength(8)]
    public required string Code { get; init; }
}

/// <summary>Represents the user information returned with an authentication response.</summary>
public sealed record UserSummaryResponse(Guid Id, string Email, string FirstName, string LastName, string Role);

/// <summary>Represents an active sign-in session.</summary>
public sealed record SessionResponse(Guid Id, string? DeviceName, DateTimeOffset CreatedAt, DateTimeOffset LastActiveAt, DateTimeOffset ExpiresAt);

/// <summary>Provides fields that can be changed on the current user's profile.</summary>
public sealed record UpdateUserRequest
{
    [MaxLength(50)]
    public string? FirstName { get; init; }

    [MaxLength(50)]
    public string? LastName { get; init; }

    [EmailAddress, MaxLength(254)]
    public string? Email { get; init; }
}

/// <summary>Represents a user's security and appearance preferences.</summary>
public sealed record SecuritySettingsResponse(bool TwoFactorEnabled, bool AutoLockEnabled, int AutoLockMinutes, string Theme, bool SecurityRemindersEnabled, DateTimeOffset UpdatedAt);

/// <summary>Provides optional changes to the current user's security settings.</summary>
public sealed record UpdateSecuritySettingsRequest
{
    public bool? TwoFactorEnabled { get; init; }
    public bool? AutoLockEnabled { get; init; }
    [Range(1, 120)]
    public int? AutoLockMinutes { get; init; }
    public string? Theme { get; init; }
    public bool? SecurityRemindersEnabled { get; init; }
}

/// <summary>Represents a category and the number of credentials assigned to it.</summary>
public sealed record CategoryResponse(Guid Id, string Name, string? Description, string Icon, string Color, int PasswordCount, DateTimeOffset CreatedAt);

/// <summary>Provides fields for creating a category.</summary>
public sealed record CreateCategoryRequest
{
    [Required, MaxLength(32)]
    public required string Name { get; init; }
    [MaxLength(90)]
    public string? Description { get; init; }
    [Required, MaxLength(8)]
    public required string Icon { get; init; }
    [Required, MaxLength(16)]
    public required string Color { get; init; }
}

/// <summary>Provides optional category changes.</summary>
public sealed record UpdateCategoryRequest
{
    [MaxLength(32)]
    public string? Name { get; init; }
    [MaxLength(90)]
    public string? Description { get; init; }
    [MaxLength(8)]
    public string? Icon { get; init; }
    [MaxLength(16)]
    public string? Color { get; init; }
}

/// <summary>Represents a tag.</summary>
public sealed record TagResponse(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Provides fields for creating a tag.</summary>
public sealed record CreateTagRequest
{
    [Required, MaxLength(32)]
    public required string Name { get; init; }
}

/// <summary>Represents a category in a credential response.</summary>
public sealed record CategorySummaryResponse(Guid Id, string Name, string Icon, string Color);

/// <summary>Represents a tag in a credential response.</summary>
public sealed record TagSummaryResponse(Guid Id, string Name);

/// <summary>Represents a credential without its password or notes.</summary>
public sealed record VaultItemResponse(Guid Id, string ServiceName, string? SiteUrl, string Username, CategorySummaryResponse? Category, IReadOnlyList<TagSummaryResponse> Tags, string IconInitial, string ColorTone, int PasswordStrengthScore, bool IsFavorite, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, long RowVersion);

/// <summary>Represents credential details without its password.</summary>
public sealed record VaultItemDetailResponse(Guid Id, string ServiceName, string? SiteUrl, string Username, string? Notes, CategorySummaryResponse? Category, IReadOnlyList<TagSummaryResponse> Tags, string IconInitial, string ColorTone, int PasswordStrengthScore, bool IsFavorite, DateTimeOffset? LastUsedAt, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, long RowVersion);

/// <summary>Provides fields for creating a credential.</summary>
public sealed record CreateVaultItemRequest
{
    [Required, MaxLength(100)]
    public required string ServiceName { get; init; }
    [Url, MaxLength(2048)]
    public string? SiteUrl { get; init; }
    [Required, EmailAddress, MaxLength(254)]
    public required string Username { get; init; }
    [Required, MaxLength(256)]
    public required string Password { get; init; }
    public Guid? CategoryId { get; init; }
    public IReadOnlyList<Guid>? TagIds { get; init; }
    [MaxLength(4000)]
    public string? Notes { get; init; }
}

/// <summary>Provides optional changes to a credential.</summary>
public sealed record UpdateVaultItemRequest
{
    [MaxLength(100)]
    public string? ServiceName { get; init; }
    [Url, MaxLength(2048)]
    public string? SiteUrl { get; init; }
    [EmailAddress, MaxLength(254)]
    public string? Username { get; init; }
    [MaxLength(256)]
    public string? Password { get; init; }
    public Guid? CategoryId { get; init; }
    public IReadOnlyList<Guid>? TagIds { get; init; }
    [MaxLength(4000)]
    public string? Notes { get; init; }
    public bool? IsFavorite { get; init; }
}

/// <summary>Provides a master password to authorize revealing a credential.</summary>
public sealed record RevealVaultItemRequest
{
    [Required, MaxLength(256)]
    public required string MasterPassword { get; init; }
}

/// <summary>Contains the revealed credential password.</summary>
public sealed record RevealVaultItemResponse(string Password, DateTimeOffset RevealedAt);

/// <summary>Represents non-secret password history metadata.</summary>
public sealed record PasswordHistoryResponse(Guid Id, int PasswordStrengthScore, DateTimeOffset ChangedAt);

/// <summary>Provides the recipient and permission for a credential share.</summary>
public sealed record CreateShareRequest
{
    [Required, EmailAddress, MaxLength(254)]
    public required string SharedWithEmail { get; init; }
    [Required]
    public required string Permission { get; init; }
}

/// <summary>Represents a user who received a shared credential.</summary>
public sealed record SharedWithResponse(Guid Id, string Email);

/// <summary>Represents an active credential share.</summary>
public sealed record ShareResponse(Guid Id, Guid VaultItemId, SharedWithResponse SharedWith, string Permission, DateTimeOffset CreatedAt);

/// <summary>Represents an audit event.</summary>
public sealed record AuditLogResponse(Guid Id, string EventType, string Detail, string Severity, DateTimeOffset CreatedAt);

/// <summary>Contains counts displayed in the audit summary.</summary>
public sealed record AuditSummaryResponse(int EventsThisMonth, int SuccessfulEvents, int NeedsReview, int CriticalEvents);

/// <summary>Represents a labeled dashboard statistic.</summary>
public sealed record DashboardStatResponse(string Label, int Value);

/// <summary>Represents a recent credential displayed on the dashboard.</summary>
public sealed record RecentVaultItemResponse(Guid Id, string ServiceName, string Username, bool IsFavorite, string ColorTone, DateTimeOffset UpdatedAt);

/// <summary>Represents a dashboard category summary.</summary>
public sealed record DashboardCategoryResponse(Guid Id, string Name, string Icon, int PasswordCount);

/// <summary>Contains aggregated data for the dashboard.</summary>
public sealed record DashboardSummaryResponse(IReadOnlyList<DashboardStatResponse> Stats, IReadOnlyList<RecentVaultItemResponse> RecentPasswords, IReadOnlyList<DashboardCategoryResponse> Categories);
