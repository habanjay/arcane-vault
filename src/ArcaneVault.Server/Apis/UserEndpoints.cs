using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class UserEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        var users = authorized.MapGroup("/users").WithTags("Users");
        users.MapGet("/me", (HttpContext context, IUserService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = service.GetUser(ApiEndpointHelpers.UserId(context));
            ApiEndpointHelpers.SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .WithName("GetCurrentUser").WithSummary("Get the current user profile")
        .Produces<UserResponse>(StatusCodes.Status200OK);

        users.MapPatch("/me", (UpdateUserRequest request, HttpContext context, IUserService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = service.UpdateUser(ApiEndpointHelpers.UserId(context), request, ApiEndpointHelpers.RequiredVersion(context));
            ApiEndpointHelpers.SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .WithName("UpdateCurrentUser").WithSummary("Update the current user profile")
        .WithDescription("Requires the current ETag in the If-Match header.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        users.MapPut("/me/photo", async (IFormFile photo, HttpContext context, IUserService service, CancellationToken cancellationToken) =>
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
                if (!ApiEndpointHelpers.IsSupportedImage(contentType, signature.AsSpan(0, bytesRead)))
                    throw new ApiException(StatusCodes.Status422UnprocessableEntity, "INVALID_PHOTO_CONTENT", "The uploaded content is not a supported image.");
            }
            var user = service.UpdateProfilePhoto(ApiEndpointHelpers.UserId(context), contentType, ApiEndpointHelpers.RequiredVersion(context));
            ApiEndpointHelpers.SetVersion(context, user.RowVersion);
            return TypedResults.Ok(user);
        })
        .Accepts<IFormFile>("multipart/form-data")
        .WithName("UpdateProfilePhoto").WithSummary("Upload a profile photo")
        .WithDescription("Requires the current ETag in the If-Match header. Image type and content are validated; maximum size is 5 MB.")
        .Produces<UserResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity)
        .Produces<ApiErrorResponse>(StatusCodes.Status428PreconditionRequired);

        users.MapDelete("/me/photo", (HttpContext context, IUserService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteProfilePhoto(ApiEndpointHelpers.UserId(context));
            return TypedResults.NoContent();
        })
        .WithName("DeleteProfilePhoto").WithSummary("Remove the profile photo")
        .Produces(StatusCodes.Status204NoContent);

        users.MapGet("/me/security-settings", (HttpContext context, IUserService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetSecuritySettings(ApiEndpointHelpers.UserId(context)));
        })
        .WithName("GetSecuritySettings").WithSummary("Get security settings")
        .Produces<SecuritySettingsResponse>(StatusCodes.Status200OK);

        users.MapPatch("/me/security-settings", (UpdateSecuritySettingsRequest request, HttpContext context, IUserService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.UpdateSecuritySettings(ApiEndpointHelpers.UserId(context), request));
        })
        .WithName("UpdateSecuritySettings").WithSummary("Update security settings")
        .Produces<SecuritySettingsResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);
    }
}
