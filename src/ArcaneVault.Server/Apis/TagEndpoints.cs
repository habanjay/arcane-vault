using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class TagEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        var tags = authorized.MapGroup("/tags").WithTags("Tags");
        tags.MapGet("", (int? page, int? pageSize, HttpContext context, ITagService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetTags(ApiEndpointHelpers.UserId(context), page ?? 1, pageSize ?? 20));
        })
        .WithName("GetTags").WithSummary("List tags")
        .Produces<PageResponse<TagResponse>>(StatusCodes.Status200OK);

        tags.MapPost("", (CreateTagRequest request, HttpContext context, ITagService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tag = service.CreateTag(ApiEndpointHelpers.UserId(context), request);
            return TypedResults.Created($"/api/v1/tags/{tag.Id}", tag);
        })
        .WithName("CreateTag").WithSummary("Create a tag")
        .Produces<TagResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict);

        tags.MapDelete("/{id:guid}", (Guid id, HttpContext context, ITagService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteTag(ApiEndpointHelpers.UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteTag").WithSummary("Delete a tag and its associations")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);
    }
}
