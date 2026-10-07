using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Apis;

internal static class CategoryEndpoints
{
    public static void Map(RouteGroupBuilder authorized)
    {
        var categories = authorized.MapGroup("/categories").WithTags("Categories");
        categories.MapGet("", (int? page, int? pageSize, HttpContext context, ICategoryService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetCategories(ApiEndpointHelpers.UserId(context), page ?? 1, pageSize ?? 20));
        })
        .WithName("GetCategories").WithSummary("List categories")
        .Produces<PageResponse<CategoryResponse>>(StatusCodes.Status200OK);

        categories.MapPost("", (CreateCategoryRequest request, HttpContext context, ICategoryService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var category = service.CreateCategory(ApiEndpointHelpers.UserId(context), request);
            return TypedResults.Created($"/api/v1/categories/{category.Id}", category);
        })
        .WithName("CreateCategory").WithSummary("Create a category")
        .Produces<CategoryResponse>(StatusCodes.Status201Created)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        categories.MapGet("/{id:guid}", (Guid id, HttpContext context, ICategoryService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.GetCategory(ApiEndpointHelpers.UserId(context), id));
        })
        .WithName("GetCategory").WithSummary("Get a category")
        .Produces<CategoryResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);

        categories.MapPatch("/{id:guid}", (Guid id, UpdateCategoryRequest request, HttpContext context, ICategoryService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return TypedResults.Ok(service.UpdateCategory(ApiEndpointHelpers.UserId(context), id, request));
        })
        .WithName("UpdateCategory").WithSummary("Update a category")
        .Produces<CategoryResponse>(StatusCodes.Status200OK)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ApiErrorResponse>(StatusCodes.Status409Conflict)
        .Produces<ApiErrorResponse>(StatusCodes.Status422UnprocessableEntity);

        categories.MapDelete("/{id:guid}", (Guid id, HttpContext context, ICategoryService service, CancellationToken cancellationToken) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            service.DeleteCategory(ApiEndpointHelpers.UserId(context), id);
            return TypedResults.NoContent();
        })
        .WithName("DeleteCategory").WithSummary("Delete a category")
        .WithDescription("Credentials in the category are left uncategorized.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces<ApiErrorResponse>(StatusCodes.Status404NotFound);
    }
}
