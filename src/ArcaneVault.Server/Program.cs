using System.Threading.RateLimiting;
using ArcaneVault.Server.Apis;
using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Mocks;
using ArcaneVault.Server.Middleware;
using ArcaneVault.Server.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer(BearerOpenApiTransformer.AddSchemeAsync);
    options.AddOperationTransformer(BearerOpenApiTransformer.AddRequirementAsync);
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddSingleton<MockTokenStore>();
builder.Services.AddSingleton<MockArcaneVaultService>();
builder.Services.AddSingleton<IAuthService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<IUserService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<ICategoryService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<ITagService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<IVaultItemService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<IAuditService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddSingleton<IDashboardService>(services => services.GetRequiredService<MockArcaneVaultService>());
builder.Services.AddAuthentication("MockBearer")
    .AddScheme<AuthenticationSchemeOptions, MockBearerHandler>("MockBearer", _ => { });
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse(new ApiError("RATE_LIMITED", "Too many requests. Try again later.")), cancellationToken);
    };
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusContext =>
{
    statusContext.HttpContext.Response.ContentType = "application/json";
    var status = statusContext.HttpContext.Response.StatusCode;
    var (code, message) = status switch
    {
        StatusCodes.Status404NotFound => ("NOT_FOUND", "The requested resource was not found."),
        StatusCodes.Status405MethodNotAllowed => ("METHOD_NOT_ALLOWED", "The HTTP method is not supported for this resource."),
        _ => ("HTTP_ERROR", "The request could not be completed.")
    };
    await statusContext.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(new ApiError(code, message)));
});
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options.WithTitle("Arcane Vault API"));
}

app.MapArcaneVaultApi();
app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
