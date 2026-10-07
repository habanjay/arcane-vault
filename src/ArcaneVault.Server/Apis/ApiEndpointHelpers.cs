using System.Security.Claims;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class ApiEndpointHelpers
{
    public static Guid UserId(HttpContext context) =>
        Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : throw new ApiException(StatusCodes.Status401Unauthorized, "INVALID_ACCESS_TOKEN", "The access token is invalid.");

    public static Guid SessionId(HttpContext context) =>
        Guid.TryParse(context.User.FindFirstValue("session_id"), out var sessionId)
            ? sessionId
            : throw new ApiException(StatusCodes.Status401Unauthorized, "INVALID_ACCESS_TOKEN", "The access token is invalid.");

    public static long RequiredVersion(HttpContext context)
    {
        var value = context.Request.Headers.IfMatch.ToString().Trim().Trim('"');
        if (long.TryParse(value, out var version) && version > 0) return version;
        throw new ApiException(StatusCodes.Status428PreconditionRequired, "IF_MATCH_REQUIRED", "A valid If-Match version is required.");
    }

    public static void SetVersion(HttpContext context, long version) =>
        context.Response.Headers.ETag = $"\"{version}\"";

    public static bool IsSupportedImage(string contentType, ReadOnlySpan<byte> bytes) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff,
        "image/png" => bytes.Length >= 8 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/gif" => bytes.Length >= 6 && (bytes[..6].SequenceEqual("GIF87a"u8) || bytes[..6].SequenceEqual("GIF89a"u8)),
        "image/webp" => bytes.Length >= 12 && bytes[..4].SequenceEqual("RIFF"u8) && bytes.Slice(8, 4).SequenceEqual("WEBP"u8),
        _ => false
    };
}
