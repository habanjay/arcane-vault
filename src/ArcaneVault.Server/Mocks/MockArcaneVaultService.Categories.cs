using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public PageResponse<CategoryResponse> GetCategories(Guid userId, int page, int pageSize)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            return Paginate(_categories.Where(category => category.UserId == userId)
                .OrderBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToCategoryResponse).ToArray(), page, pageSize);
        }
    }
    public CategoryResponse GetCategory(Guid userId, Guid id)
    {
        lock (_gate)
        {
            return ToCategoryResponse(FindCategory(userId, id));
        }
    }
    public CategoryResponse CreateCategory(Guid userId, CreateCategoryRequest request)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            ValidateColor(request.Color);
            EnsureUniqueCategoryName(userId, request.Name, null);
            var category = new CategoryRecord(Guid.NewGuid(), userId, request.Name.Trim(), request.Description?.Trim(), request.Icon, request.Color, DateTimeOffset.UtcNow);
            _categories.Add(category);
            return ToCategoryResponse(category);
        }
    }
    public CategoryResponse UpdateCategory(Guid userId, Guid id, UpdateCategoryRequest request)
    {
        lock (_gate)
        {
            var category = FindCategory(userId, id);
            if (request.Color is not null) ValidateColor(request.Color);
            if (request.Name is not null)
            {
                EnsureUniqueCategoryName(userId, request.Name, id);
                category.Name = request.Name.Trim();
            }
            if (request.Description is not null) category.Description = request.Description.Trim();
            if (request.Icon is not null) category.Icon = request.Icon;
            if (request.Color is not null) category.Color = request.Color;
            return ToCategoryResponse(category);
        }
    }
    public void DeleteCategory(Guid userId, Guid id)
    {
        lock (_gate)
        {
            var category = FindCategory(userId, id);
            _categories.Remove(category);
            foreach (var item in _vaultItems.Where(item => item.UserId == userId && item.CategoryId == id))
            {
                item.CategoryId = null;
                item.Version++;
            }
        }
    }
}
