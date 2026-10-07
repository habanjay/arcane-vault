using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace ArcaneVault.Server.Apis;

internal static class AuthenticationEndpoints
{
    public static void Map(RouteGroupBuilder anonymous, RouteGroupBuilder authorized)
    {
        anonymous.MapPost("/register", (RegisterRequest request, IAuthService service, CancellationToken cancellationToken) =>
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

        anonymous.MapPost("/login", Results<Ok<LoginResponse>, Accepted<TwoFactorChallengeResponse>>
            (LoginRequest request, HttpContext context, IAuthService service, CancellationToken cancellationToken) =>
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

        anonymous.MapPost("/refresh", (RefreshRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.Refresh(request));
        })
        .AllowAnonymous().WithName("RefreshTokens").WithSummary("Rotate authentication tokens")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        anonymous.MapPost("/2fa/verify", (TwoFactorVerifyRequest request, IAuthService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.VerifyTwoFactor(request));
        })
        .AllowAnonymous().RequireRateLimiting("sensitive").WithName("VerifyTwoFactor").WithSummary("Complete two-factor sign-in")
        .WithDescription("The mock implementation accepts the demo verification code 123456.")
        .Produces<LoginResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status401Unauthorized);

        authorized.MapPost("/logout", (HttpContext context, IAuthService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.Logout(ApiEndpointHelpers.UserId(context), ApiEndpointHelpers.SessionId(context));
            return TypedResults.NoContent();
        })
        .WithName("Logout").WithSummary("Sign out")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        authorized.MapGet("/sessions", (HttpContext context, IAuthService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetSessions(ApiEndpointHelpers.UserId(context)));
        })
        .WithName("GetSessions").WithSummary("List active sessions")
        .Produces<IReadOnlyList<SessionResponse>>(StatusCodes.Status200OK);

        authorized.MapDelete("/sessions/{sessionId:guid}", (Guid sessionId, HttpContext context, IAuthService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.RevokeSession(ApiEndpointHelpers.UserId(context), sessionId);
            return TypedResults.NoContent();
        })
        .WithName("RevokeSession").WithSummary("Revoke a session")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);
    }
}
