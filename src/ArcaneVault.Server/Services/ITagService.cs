using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface ITagService
{
    PageResponse<TagResponse> GetTags(Guid userId, int page, int pageSize);
    TagResponse CreateTag(Guid userId, CreateTagRequest request);
    void DeleteTag(Guid userId, Guid id);
}
