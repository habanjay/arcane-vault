using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Dtos;

/// <summary>Represents a user profile.</summary>
public sealed record UserResponse(Guid Id, string Email, string FirstName, string LastName, string Role, string? ProfilePhotoUrl, DateTimeOffset CreatedAt, DateTimeOffset? LastLoginAt, long RowVersion);

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
