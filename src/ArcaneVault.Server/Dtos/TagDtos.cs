using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Dtos;

/// <summary>Represents a tag.</summary>
public sealed record TagResponse(Guid Id, string Name, DateTimeOffset CreatedAt);

/// <summary>Provides fields for creating a tag.</summary>
public sealed record CreateTagRequest
{
    [Required, MaxLength(32)]
    public required string Name { get; init; }
}
