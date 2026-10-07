using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Dtos;

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
