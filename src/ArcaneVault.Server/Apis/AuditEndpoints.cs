using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class AuditEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        var audit = authorized.MapGroup("/audit-logs").WithTags("Audit log");
        audit.MapGet("", (string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize,
            HttpContext context, IAuditService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetAuditLogs(ApiEndpointHelpers.UserId(context), severity, eventType, from, to, page ?? 1, pageSize ?? 20));
        })
        .WithName("GetAuditLogs").WithSummary("List audit events")
        .Produces<PageResponse<AuditLogResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        audit.MapGet("/summary", (HttpContext context, IAuditService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetAuditSummary(ApiEndpointHelpers.UserId(context)));
        })
        .WithName("GetAuditSummary").WithSummary("Get audit summary counts")
        .Produces<AuditSummaryResponse>(StatusCodes.Status200OK);
    }
}
