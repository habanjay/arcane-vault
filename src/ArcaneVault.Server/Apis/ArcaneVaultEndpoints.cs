namespace ArcaneVault.Server.Apis;

public static class ArcaneVaultEndpoints
{
    public static RouteGroupBuilder MapArcaneVaultApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1").AddEndpointFilter<ApiValidationFilter>();
        var anonymousAuth = api.MapGroup("/auth").WithTags("Authentication");
        var authorized = api.MapGroup("").RequireAuthorization();
        var authorizedAuth = authorized.MapGroup("/auth").WithTags("Authentication");

        AuthenticationEndpoints.Map(anonymousAuth, authorizedAuth);
        UserEndpoints.Map(authorized);
        CategoryEndpoints.Map(authorized);
        TagEndpoints.Map(authorized);
        VaultItemEndpoints.Map(authorized);
        AuditEndpoints.Map(authorized);
        DashboardEndpoints.Map(authorized);

        return api;
    }
}
