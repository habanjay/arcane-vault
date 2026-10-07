using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class VaultItemEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        var vault = authorized.MapGroup("/vault-items").WithTags("Vault items");
        vault.MapGet("", (Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int? page, int? pageSize,
            HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetVaultItems(ApiEndpointHelpers.UserId(context), category, tag, favorite, search, sort, page ?? 1, pageSize ?? 20));
        })
        .WithName("GetVaultItems").WithSummary("Search and filter credentials")
        .Produces<PageResponse<VaultItemResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        vault.MapPost("", (CreateVaultItemRequest request, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.CreateVaultItem(ApiEndpointHelpers.UserId(context), request);
            return TypedResults.Created($"/api/v1/vault-items/{item.Id}", item);
        })
        .WithName("CreateVaultItem").WithSummary("Create a credential")
        .WithDescription("The mock service keeps secrets in memory only. Do not use it as production secret storage.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapGet("/{id:guid}", (Guid id, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.GetVaultItem(ApiEndpointHelpers.UserId(context), id);
            ApiEndpointHelpers.SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("GetVaultItem").WithSummary("Get credential details")
        .WithDescription("The password is never included in this response.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPatch("/{id:guid}", (Guid id, UpdateVaultItemRequest request, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.UpdateVaultItem(ApiEndpointHelpers.UserId(context), id, request, ApiEndpointHelpers.RequiredVersion(context));
            ApiEndpointHelpers.SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("UpdateVaultItem").WithSummary("Update a credential")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        vault.MapDelete("/{id:guid}", (Guid id, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteVaultItem(ApiEndpointHelpers.UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteVaultItem").WithSummary("Delete a credential")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPatch("/{id:guid}/favorite", (Guid id, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.ToggleFavorite(ApiEndpointHelpers.UserId(context), id, ApiEndpointHelpers.RequiredVersion(context));
            ApiEndpointHelpers.SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("ToggleFavorite").WithSummary("Toggle a credential favorite")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<VaultItemResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        vault.MapPost("/{id:guid}/reveal", (Guid id, RevealVaultItemRequest request, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.RevealVaultItem(ApiEndpointHelpers.UserId(context), id, request.MasterPassword));
        })
        .RequireRateLimiting("sensitive").WithName("RevealVaultItem").WithSummary("Reveal a credential password")
        .WithDescription("Requires the master password again. Every reveal is audited.")
        .Produces<RevealVaultItemResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests);

        vault.MapGet("/{id:guid}/history", (Guid id, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetPasswordHistory(ApiEndpointHelpers.UserId(context), id));
        })
        .WithName("GetPasswordHistory").WithSummary("List password history metadata")
        .Produces<IReadOnlyList<PasswordHistoryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPost("/{id:guid}/shares", (Guid id, CreateShareRequest request, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var share = service.CreateShare(ApiEndpointHelpers.UserId(context), id, request);
            return TypedResults.Created($"/api/v1/vault-items/{id}/shares/{share.Id}", share);
        })
        .WithName("CreateShare").WithSummary("Share a credential")
        .Produces<ShareResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        vault.MapGet("/{id:guid}/shares", (Guid id, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetShares(ApiEndpointHelpers.UserId(context), id));
        })
        .WithName("GetShares").WithSummary("List active credential shares")
        .Produces<IReadOnlyList<ShareResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapDelete("/{id:guid}/shares/{shareId:guid}", (Guid id, Guid shareId, HttpContext context, IVaultItemService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteShare(ApiEndpointHelpers.UserId(context), id, shareId);
            return TypedResults.NoContent();
        })
        .WithName("RevokeShare").WithSummary("Revoke a credential share")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);
    }
}
