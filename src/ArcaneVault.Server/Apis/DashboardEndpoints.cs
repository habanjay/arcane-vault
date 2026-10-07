using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class DashboardEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        authorized.MapGet("/dashboard/summary", (HttpContext context, IDashboardService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetDashboardSummary(ApiEndpointHelpers.UserId(context)));
        })
        .WithTags("Dashboard").WithName("GetDashboardSummary").WithSummary("Get dashboard summary")
        .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK);
    }
}
