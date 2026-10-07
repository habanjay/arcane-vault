namespace ArcaneVault.Server.Dtos;

/// <summary>Represents a paginated collection.</summary>
public sealed record PageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

/// <summary>Describes a validation issue in an API error response.</summary>
public sealed record ApiErrorDetail(string Field, string Issue);

/// <summary>Provides the documented error response envelope.</summary>
public sealed record ApiErrorResponse(ApiError Error);

/// <summary>Describes an API error.</summary>
public sealed record ApiError(string Code, string Message, IReadOnlyList<ApiErrorDetail>? Details = null);
