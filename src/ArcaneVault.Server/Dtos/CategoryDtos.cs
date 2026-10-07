using System.ComponentModel.DataAnnotations;

namespace ArcaneVault.Server.Dtos;

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
