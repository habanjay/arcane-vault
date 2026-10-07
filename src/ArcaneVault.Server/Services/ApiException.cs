using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public sealed class ApiException(int statusCode, string code, string message, IReadOnlyList<ApiErrorDetail>? details = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
    public IReadOnlyList<ApiErrorDetail>? Details { get; } = details;
}
