using ArcaneVault.Server.Middleware;
using ArcaneVault.Server.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;

namespace ArcaneVault.Server.Tests.TestSupport;

internal sealed class ApiExceptionHandlerTestContext : IAsyncDisposable
{
    public ApiExceptionHandlerTestContext()
    {
        Logger = new RecordingLogger<ApiExceptionHandler>();
        Handler = new ApiExceptionHandler(Logger);
        HttpContext = new DefaultHttpContext();
        ResponseBody = new MemoryStream();
        HttpContext.Response.Body = ResponseBody;
    }

    public RecordingLogger<ApiExceptionHandler> Logger { get; }

    public ApiExceptionHandler Handler { get; }

    public DefaultHttpContext HttpContext { get; }

    public MemoryStream ResponseBody { get; }

    public ValueTask DisposeAsync() => ResponseBody.DisposeAsync();
}
