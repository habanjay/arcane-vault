using System.Text.Json;
using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Middleware;
using ArcaneVault.Server.Services;
using ArcaneVault.Server.Tests.TestDoubles;
using ArcaneVault.Server.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ArcaneVault.Server.Tests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ApiException_WritesErrorResponse()
    {
        await using var scenario = new ApiExceptionHandlerTestContext();

        var apiException = new ApiException(
            StatusCodes.Status400BadRequest,
            "INVALID_REQUEST",
            "The request is invalid.",
            [new ApiErrorDetail("name", "The field is required.")]);

        var handled = await scenario.Handler.TryHandleAsync(
            scenario.HttpContext,
            apiException,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, scenario.HttpContext.Response.StatusCode);

        scenario.ResponseBody.Position = 0;
        var response = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            scenario.ResponseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(response);
        Assert.Equal("INVALID_REQUEST", response.Error.Code);
        Assert.Equal("The request is invalid.", response.Error.Message);
        Assert.Equal([new ApiErrorDetail("name", "The field is required.")], response.Error.Details);
    }

    [Fact]
    public async Task TryHandleAsync_NonApiException_DoesNotHandleException()
    {
        await using var scenario = new ApiExceptionHandlerTestContext();

        var handled = await scenario.Handler.TryHandleAsync(
            scenario.HttpContext,
            new InvalidOperationException("Unexpected failure."),
            CancellationToken.None);

        Assert.False(handled);
        Assert.Equal(StatusCodes.Status200OK, scenario.HttpContext.Response.StatusCode);
        Assert.Equal(0, scenario.ResponseBody.Length);
    }

    [Fact]
    public async Task TryHandleAsync_ServerError_LogsException()
    {
        await using var scenario = new ApiExceptionHandlerTestContext();
        var apiException = new ApiException(
            StatusCodes.Status500InternalServerError,
            "DATABASE_FAILURE",
            "The request could not be completed.");

        var handled = await scenario.Handler.TryHandleAsync(
            scenario.HttpContext,
            apiException,
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, scenario.HttpContext.Response.StatusCode);
        var log = Assert.Single(scenario.Logger.Entries);
        Assert.Equal(LogLevel.Error, log.Level);
        Assert.Contains("DATABASE_FAILURE", log.Message, StringComparison.Ordinal);
        Assert.Same(apiException, log.Exception);
    }
}
