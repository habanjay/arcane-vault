using System.Security.Claims;
using ArcaneVault.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ArcaneVault.Server.Apis;

public static class ArcaneVaultEndpoints
{
    public static RouteGroupBuilder MapArcaneVaultApi(this IEndpointRouteBuilder endpoints)
    {
        var api = endpoints.MapGroup("/api/v1").AddEndpointFilter<ApiValidationFilter>();
        var auth = api.MapGroup("/auth").WithTags("Authentication");

        auth.MapPost("/register", (RegisterRequest request, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = service.Register(request);
            return TypedResults.Created($"/api/v1/users/{user.Id}", user);
        })
        .AllowAnonymous().RequireRateLimiting("sensitive").WithName("Register")
        .WithSummary("Create an account")
        .WithDescription("Creates a local mock account. Master passwords are hashed before being stored by the mock service.")
        .Produces<UserResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict);

        auth.MapPost("/login", Results<Ok<LoginResponse>, Accepted<TwoFactorChallengeResponse>>
            (LoginRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (response, challenge) = service.Login(request, request.DeviceName ?? context.Request.Headers.UserAgent.ToString());
            return response is not null
                ? TypedResults.Ok(response)
                : TypedResults.Accepted("/api/v1/auth/2fa/verify", challenge!);
        })
        .AllowAnonymous().RequireRateLimiting("sensitive").WithName("Login")
        .WithSummary("Sign in")
        .WithDescription("Exchanges credentials for mock bearer and refresh tokens, or returns a second-factor challenge.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<TwoFactorChallengeResponse>(StatusCodes.Status202Accepted)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        auth.MapPost("/refresh", (RefreshRequest request, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.Refresh(request));
        })
        .AllowAnonymous().WithName("RefreshTokens").WithSummary("Rotate authentication tokens")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        auth.MapPost("/2fa/verify", (TwoFactorVerifyRequest request, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.VerifyTwoFactor(request));
        })
        .AllowAnonymous().RequireRateLimiting("sensitive").WithName("VerifyTwoFactor").WithSummary("Complete two-factor sign-in")
        .WithDescription("The mock implementation accepts the demo verification code 123456.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        var secured = api.MapGroup("").RequireAuthorization();
        var securedAuth = secured.MapGroup("/auth").WithTags("Authentication");
        securedAuth.MapPost("/logout", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.Logout(UserId(context), SessionId(context));
            return TypedResults.NoContent();
        })
        .WithName("Logout").WithSummary("Sign out")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        securedAuth.MapGet("/sessions", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetSessions(UserId(context)));
        })
        .WithName("GetSessions").WithSummary("List active sessions")
        .Produces<IReadOnlyList<SessionResponse>>(StatusCodes.Status200OK);

        securedAuth.MapDelete("/sessions/{sessionId:guid}", (Guid sessionId, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.RevokeSession(UserId(context), sessionId);
            return TypedResults.NoContent();
        })
        .WithName("RevokeSession").WithSummary("Revoke a session")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var users = secured.MapGroup("/users").WithTags("Users");
        users.MapGet("/me", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = service.GetUser(UserId(context));
            SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .WithName("GetCurrentUser").WithSummary("Get the current user profile")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        users.MapPatch("/me", (UpdateUserRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = service.UpdateUser(UserId(context), request, RequiredVersion(context));
            SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .WithName("UpdateCurrentUser").WithSummary("Update the current user profile")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        users.MapPut("/me/photo", async (IFormFile photo, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (photo.Length is < 1 or > 5 * 1024 * 1024)
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_PHOTO_SIZE", "The profile photo must be no larger than 5 MB.");
            var contentType = photo.ContentType.ToLowerInvariant();
            if (contentType is not ("image/jpeg" or "image/png" or "image/gif" or "image/webp"))
                throw new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_PHOTO_TYPE", "The profile photo must be a JPEG, PNG, GIF, or WebP image.");
            var signature = new byte[12];
            await using (var stream = photo.OpenReadStream())
            {
                var bytesRead = await stream.ReadAsync(signature, cancellationToken);
                if (!IsSupportedImage(contentType, signature.AsSpan(0, bytesRead)))
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_PHOTO_CONTENT", "The uploaded content is not a supported image.");
            }
            var user = service.UpdateProfilePhoto(UserId(context), contentType, RequiredVersion(context));
            SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .WithName("UpdateProfilePhoto").WithSummary("Upload a profile photo")
        .WithDescription("Requires the current ETag in the If-Match header. Image type and content are validated; maximum size is 5 MB.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        users.MapDelete("/me/photo", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteProfilePhoto(UserId(context));
            return TypedResults.NoContent();
        })
        .WithName("DeleteProfilePhoto").WithSummary("Remove the profile photo")
        .Produces(StatusCodes.Status204NoContent);

        users.MapGet("/me/security-settings", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetSecuritySettings(UserId(context)));
        })
        .WithName("GetSecuritySettings").WithSummary("Get security settings")
        .Produces<SecuritySettingsResponse>(StatusCodes.Status200OK);

        users.MapPatch("/me/security-settings", (UpdateSecuritySettingsRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.UpdateSecuritySettings(UserId(context), request));
        })
        .WithName("UpdateSecuritySettings").WithSummary("Update security settings")
        .Produces<SecuritySettingsResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        var categories = secured.MapGroup("/categories").WithTags("Categories");
        categories.MapGet("", (int? page, int? pageSize, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetCategories(UserId(context), page ?? 1, pageSize ?? 20));
        })
        .WithName("GetCategories").WithSummary("List categories")
        .Produces<PageResponse<CategoryResponse>>(StatusCodes.Status200OK);

        categories.MapPost("", (CreateCategoryRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = service.CreateCategory(UserId(context), request);
            return TypedResults.Created($"/api/v1/categories/{category.Id}", category);
        })
        .WithName("CreateCategory").WithSummary("Create a category")
        .Produces<CategoryResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        categories.MapGet("/{id:guid}", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetCategory(UserId(context), id));
        })
        .WithName("GetCategory").WithSummary("Get a category")
        .Produces<CategoryResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        categories.MapPatch("/{id:guid}", (Guid id, UpdateCategoryRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.UpdateCategory(UserId(context), id, request));
        })
        .WithName("UpdateCategory").WithSummary("Update a category")
        .Produces<CategoryResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        categories.MapDelete("/{id:guid}", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteCategory(UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteCategory").WithSummary("Delete a category")
        .WithDescription("Credentials in the category are left uncategorized.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var tags = secured.MapGroup("/tags").WithTags("Tags");
        tags.MapGet("", (int? page, int? pageSize, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetTags(UserId(context), page ?? 1, pageSize ?? 20));
        })
        .WithName("GetTags").WithSummary("List tags")
        .Produces<PageResponse<TagResponse>>(StatusCodes.Status200OK);

        tags.MapPost("", (CreateTagRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tag = service.CreateTag(UserId(context), request);
            return TypedResults.Created($"/api/v1/tags/{tag.Id}", tag);
        })
        .WithName("CreateTag").WithSummary("Create a tag")
        .Produces<TagResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict);

        tags.MapDelete("/{id:guid}", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteTag(UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteTag").WithSummary("Delete a tag and its associations")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var vault = secured.MapGroup("/vault-items").WithTags("Vault items");
        vault.MapGet("", (Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int? page, int? pageSize,
            HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetVaultItems(UserId(context), category, tag, favorite, search, sort, page ?? 1, pageSize ?? 20));
        })
        .WithName("GetVaultItems").WithSummary("Search and filter credentials")
        .Produces<PageResponse<VaultItemResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        vault.MapPost("", (CreateVaultItemRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.CreateVaultItem(UserId(context), request);
            return TypedResults.Created($"/api/v1/vault-items/{item.Id}", item);
        })
        .WithName("CreateVaultItem").WithSummary("Create a credential")
        .WithDescription("The mock service keeps secrets in memory only. Do not use it as production secret storage.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapGet("/{id:guid}", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.GetVaultItem(UserId(context), id);
            SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("GetVaultItem").WithSummary("Get credential details")
        .WithDescription("The password is never included in this response.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPatch("/{id:guid}", (Guid id, UpdateVaultItemRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.UpdateVaultItem(UserId(context), id, request, RequiredVersion(context));
            SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("UpdateVaultItem").WithSummary("Update a credential")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<VaultItemDetailResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        vault.MapDelete("/{id:guid}", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteVaultItem(UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteVaultItem").WithSummary("Delete a credential")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPatch("/{id:guid}/favorite", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var item = service.ToggleFavorite(UserId(context), id, RequiredVersion(context));
            SetVersion(context, item.RowVersion);
            return TypedResults.Ok(item);
        })
        .WithName("ToggleFavorite").WithSummary("Toggle a credential favorite")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<VaultItemResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        vault.MapPost("/{id:guid}/reveal", (Guid id, RevealVaultItemRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.RevealVaultItem(UserId(context), id, request.MasterPassword));
        })
        .RequireRateLimiting("sensitive").WithName("RevealVaultItem").WithSummary("Reveal a credential password")
        .WithDescription("Requires the master password again. Every reveal is audited.")
        .Produces<RevealVaultItemResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests);

        vault.MapGet("/{id:guid}/history", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetPasswordHistory(UserId(context), id));
        })
        .WithName("GetPasswordHistory").WithSummary("List password history metadata")
        .Produces<IReadOnlyList<PasswordHistoryResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapPost("/{id:guid}/shares", (Guid id, CreateShareRequest request, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var share = service.CreateShare(UserId(context), id, request);
            return TypedResults.Created($"/api/v1/vault-items/{id}/shares/{share.Id}", share);
        })
        .WithName("CreateShare").WithSummary("Share a credential")
        .Produces<ShareResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        vault.MapGet("/{id:guid}/shares", (Guid id, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetShares(UserId(context), id));
        })
        .WithName("GetShares").WithSummary("List active credential shares")
        .Produces<IReadOnlyList<ShareResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        vault.MapDelete("/{id:guid}/shares/{shareId:guid}", (Guid id, Guid shareId, HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteShare(UserId(context), id, shareId);
            return TypedResults.NoContent();
        })
        .WithName("RevokeShare").WithSummary("Revoke a credential share")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        var audit = secured.MapGroup("/audit-logs").WithTags("Audit log");
        audit.MapGet("", (string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize,
            HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetAuditLogs(UserId(context), severity, eventType, from, to, page ?? 1, pageSize ?? 20));
        })
        .WithName("GetAuditLogs").WithSummary("List audit events")
        .Produces<PageResponse<AuditLogResponse>>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status400BadRequest);

        audit.MapGet("/summary", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetAuditSummary(UserId(context)));
        })
        .WithName("GetAuditSummary").WithSummary("Get audit summary counts")
        .Produces<AuditSummaryResponse>(StatusCodes.Status200OK);

        secured.MapGet("/dashboard/summary", (HttpContext context, IArcaneVaultService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetDashboardSummary(UserId(context)));
        })
        .WithTags("Dashboard").WithName("GetDashboardSummary").WithSummary("Get dashboard summary")
        .Produces<DashboardSummaryResponse>(StatusCodes.Status200OK);

        return api;
    }

    private static Guid UserId(HttpContext context) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new ApiException(StatusCodes.Status401Unauthorized, "INVALID_ACCESS_TOKEN", "The access token is invalid.");

    private static Guid SessionId(HttpContext context) =>
        Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId)
            ? sessionId
            : throw new ApiException(StatusCodes.Status401Unauthorized, "INVALID_ACCESS_TOKEN", "The access token is invalid.");

    private static long RequiredVersion(HttpContext context)
    {
        var value = context.Request.Headers.IfMatch.ToString().Trim().Trim('"');
        if (long.TryParse(value, out var version) && version > 0) return version;
        throw new ApiException(StatusCodes.Status428PreconditionRequired, "IF_MATCH_REQUIRED", "A valid If-Match version is required.");
    }

    private static void SetVersion(HttpContext context, long version) =>
        context.Response.Headers.ETag = $"\"{version}\"";

    private static bool IsSupportedImage(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/gif" => bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
