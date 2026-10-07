using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public PageResponse<TagResponse> GetTags(Guid userId, int page, int pageSize)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            return Paginate(_tags.Where(tag => tag.UserId == userId).OrderBy(tag => tag.Name, StringComparer.OrdinalIgnoreCase)
                .Select(ToTagResponse).ToArray(), page, pageSize);
        }
    }
    public TagResponse CreateTag(Guid userId, CreateTagRequest request)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            var name = request.Name.Trim();
            if (_tags.Any(tag => tag.UserId == userId && tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                throw Conflict("TAG_NAME_EXISTS", "A tag with that name already exists.");
            }
            var tag = new TagRecord(Guid.NewGuid(), userId, name, DateTimeOffset.UtcNow);
            _tags.Add(tag);
            return ToTagResponse(tag);
        }
    }
    public void DeleteTag(Guid userId, Guid id)
    {
        lock (_gate)
        {
            var tag = FindTag(userId, id);
            _tags.Remove(tag);
            foreach (var item in _vaultItems.Where(item => item.UserId == userId))
            {
                item.TagIds.Remove(id);
            }
        }
    }
}
