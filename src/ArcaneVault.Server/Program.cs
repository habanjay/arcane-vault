using System.Threading.RateLimiting;
using ArcaneVault.Server.Apis;
using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Middleware;
using ArcaneVault.Server.Services;
using Microsoft.AspNetCore.DataProtection;
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
var configuredKeyDirectory = builder.Configuration["DataProtection:KeyDirectory"];
var keyDirectory = string.IsNullOrWhiteSpace(configuredKeyDirectory)
    ? Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")
    : Path.IsPathRooted(configuredKeyDirectory)
        ? configuredKeyDirectory
        : Path.Combine(builder.Environment.ContentRootPath, configuredKeyDirectory);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDirectory))
    .SetApplicationName("ArcaneVault");
builder.Services.AddScoped<SqlArcaneVaultService>();
builder.Services.AddScoped<IAuthService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<IAccessTokenValidator>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<IUserService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<ICategoryService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<ITagService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<IVaultItemService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<IAuditService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddScoped<IDashboardService>(services => services.GetRequiredService<SqlArcaneVaultService>());
builder.Services.AddAuthentication("Bearer")
    .AddScheme<AuthenticationSchemeOptions, SqlBearerHandler>("Bearer", _ => { });
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
