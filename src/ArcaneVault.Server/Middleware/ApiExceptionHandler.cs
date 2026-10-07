using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;
using Microsoft.AspNetCore.Diagnostics;

namespace ArcaneVault.Server.Middleware;

internal sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ApiException apiException)
        {
            return false;
        }

        if (apiException.StatusCode >= 500)
        {
            logger.LogError(exception, "Unhandled API error {ErrorCode}", apiException.Code);
        }

        httpContext.Response.StatusCode = apiException.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(
            new ApiError(apiException.Code, apiException.Message, apiException.Details)), cancellationToken);
        return true;
    }
}
