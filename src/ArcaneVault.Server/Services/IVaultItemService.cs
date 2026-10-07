using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IVaultItemService
{
    PageResponse<VaultItemResponse> GetVaultItems(Guid userId, Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int page, int pageSize);
    VaultItemDetailResponse GetVaultItem(Guid userId, Guid id);
    VaultItemDetailResponse CreateVaultItem(Guid userId, CreateVaultItemRequest request);
    VaultItemDetailResponse UpdateVaultItem(Guid userId, Guid id, UpdateVaultItemRequest request, long expectedVersion);
    void DeleteVaultItem(Guid userId, Guid id);
    VaultItemResponse ToggleFavorite(Guid userId, Guid id, long expectedVersion);
    RevealVaultItemResponse RevealVaultItem(Guid userId, Guid id, string masterPassword);
    IReadOnlyList<PasswordHistoryResponse> GetPasswordHistory(Guid userId, Guid id);
    ShareResponse CreateShare(Guid userId, Guid id, CreateShareRequest request);
    IReadOnlyList<ShareResponse> GetShares(Guid userId, Guid id);
    void DeleteShare(Guid userId, Guid id, Guid shareId);
}
