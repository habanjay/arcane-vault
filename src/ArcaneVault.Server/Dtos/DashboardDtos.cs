namespace ArcaneVault.Server.Dtos;

/// <summary>Represents a labeled dashboard statistic.</summary>
public sealed record DashboardStatResponse(string Label, int Value);

/// <summary>Represents a recent credential displayed on the dashboard.</summary>
public sealed record RecentVaultItemResponse(Guid Id, string ServiceName, string Username, bool IsFavorite, string ColorTone, DateTimeOffset UpdatedAt);

/// <summary>Represents a dashboard category summary.</summary>
public sealed record DashboardCategoryResponse(Guid Id, string Name, string Icon, int PasswordCount);

/// <summary>Contains aggregated data for the dashboard.</summary>
public sealed record DashboardSummaryResponse(IReadOnlyList<DashboardStatResponse> Stats, IReadOnlyList<RecentVaultItemResponse> RecentPasswords, IReadOnlyList<DashboardCategoryResponse> Categories);
