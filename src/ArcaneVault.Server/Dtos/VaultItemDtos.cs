using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Dtos;

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
