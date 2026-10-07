using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface ICategoryService
{
    PageResponse<CategoryResponse> GetCategories(Guid userId, int page, int pageSize);
    CategoryResponse GetCategory(Guid userId, Guid id);
    CategoryResponse CreateCategory(Guid userId, CreateCategoryRequest request);
    CategoryResponse UpdateCategory(Guid userId, Guid id, UpdateCategoryRequest request);
    void DeleteCategory(Guid userId, Guid id);
}
