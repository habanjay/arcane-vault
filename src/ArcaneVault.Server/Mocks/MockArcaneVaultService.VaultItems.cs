using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
    public PageResponse<VaultItemResponse> GetVaultItems(Guid userId, Guid? category, Guid? tag, bool? favorite, string? search, string? sort, int page, int pageSize)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            if (category is { } categoryId) FindCategory(userId, categoryId);
            if (tag is { } tagId) FindTag(userId, tagId);
            IEnumerable<VaultRecord> query = _vaultItems.Where(item => item.UserId == userId);
            if (category is { } categoryFilterId) query = query.Where(item => item.CategoryId == categoryFilterId);
            if (tag is { } tagFilterId) query = query.Where(item => item.TagIds.Contains(tagFilterId));
            if (favorite is { } isFavorite) query = query.Where(item => item.IsFavorite == isFavorite);
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(item => item.ServiceName.Contains(search, StringComparison.OrdinalIgnoreCase)
                    || item.Username.Contains(search, StringComparison.OrdinalIgnoreCase));
            }

            query = sort switch
            {
                null or "-updatedAt" => query.OrderByDescending(item => item.UpdatedAt),
                "updatedAt" => query.OrderBy(item => item.UpdatedAt),
                "serviceName" => query.OrderBy(item => item.ServiceName, StringComparer.OrdinalIgnoreCase),
                _ => throw BadRequest("INVALID_SORT", "sort must be updatedAt, serviceName, or -updatedAt.")
            };
            return Paginate(query.Select(ToVaultItemResponse).ToArray(), page, pageSize);
        }
    }
    public VaultItemDetailResponse GetVaultItem(Guid userId, Guid id)
    {
        lock (_gate)
        {
            return ToVaultItemDetail(FindVaultItem(userId, id));
        }
    }
    public VaultItemDetailResponse CreateVaultItem(Guid userId, CreateVaultItemRequest request)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            if (request.CategoryId is { } categoryId) FindCategory(userId, categoryId);
            var tagIds = ValidateTagIds(userId, request.TagIds);
            var now = DateTimeOffset.UtcNow;
            var item = new VaultRecord(Guid.NewGuid(), userId, request.ServiceName.Trim(), request.SiteUrl, request.Username.Trim(),
                request.Password, request.Notes, request.CategoryId, tagIds, StrengthScore(request.Password), false, null, now, now, 1);
            _vaultItems.Add(item);
            AddAudit(userId, "PasswordCreated", item.ServiceName, "Success", item.Id);
            return ToVaultItemDetail(item);
        }
    }
    public VaultItemDetailResponse UpdateVaultItem(Guid userId, Guid id, UpdateVaultItemRequest request, long expectedVersion)
    {
        lock (_gate)
        {
            var item = FindVaultItem(userId, id);
            CheckVersion(item.Version, expectedVersion);
            if (request.ServiceName is not null) item.ServiceName = request.ServiceName.Trim();
            if (request.SiteUrl is not null) item.SiteUrl = request.SiteUrl;
            if (request.Username is not null) item.Username = request.Username.Trim();
            if (request.Password is not null)
            {
                item.History.Insert(0, new PasswordHistoryResponse(Guid.NewGuid(), item.PasswordStrengthScore, DateTimeOffset.UtcNow));
                item.Password = request.Password;
                item.PasswordStrengthScore = StrengthScore(request.Password);
            }
            if (request.CategoryId is { } categoryId)
            {
                FindCategory(userId, categoryId);
                item.CategoryId = categoryId;
            }
            if (request.TagIds is not null) item.TagIds = ValidateTagIds(userId, request.TagIds);
            if (request.Notes is not null) item.Notes = request.Notes;
            if (request.IsFavorite is { } isFavorite) item.IsFavorite = isFavorite;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.Version++;
            AddAudit(userId, "PasswordUpdated", item.ServiceName, "Success", item.Id);
            return ToVaultItemDetail(item);
        }
    }
    public void DeleteVaultItem(Guid userId, Guid id)
    {
        lock (_gate)
        {
            var item = FindVaultItem(userId, id);
            _vaultItems.Remove(item);
            _shares.RemoveAll(share => share.VaultItemId == id);
        }
    }
    public VaultItemResponse ToggleFavorite(Guid userId, Guid id, long expectedVersion)
    {
        lock (_gate)
        {
            var item = FindVaultItem(userId, id);
            CheckVersion(item.Version, expectedVersion);
            item.IsFavorite = !item.IsFavorite;
            item.UpdatedAt = DateTimeOffset.UtcNow;
            item.Version++;
            return ToVaultItemResponse(item);
        }
    }
    public RevealVaultItemResponse RevealVaultItem(Guid userId, Guid id, string masterPassword)
    {
        lock (_gate)
        {
            var user = FindUser(userId);
            if (!VerifyPassword(masterPassword, user.PasswordSalt, user.PasswordHash))
            {
                throw Unauthorized("INVALID_CREDENTIALS", "The master password is incorrect.");
            }
            var item = FindVaultItem(userId, id);
            var revealedAt = DateTimeOffset.UtcNow;
            item.LastUsedAt = revealedAt;
            AddAudit(userId, "PasswordViewed", item.ServiceName, "Success", item.Id);
            return new RevealVaultItemResponse(item.Password, revealedAt);
        }
    }
    public IReadOnlyList<PasswordHistoryResponse> GetPasswordHistory(Guid userId, Guid id)
    {
        lock (_gate)
        {
            return FindVaultItem(userId, id).History.ToArray();
        }
    }
    public ShareResponse CreateShare(Guid userId, Guid id, CreateShareRequest request)
    {
        lock (_gate)
        {
            var item = FindVaultItem(userId, id);
            if (request.Permission is not ("View" or "Edit"))
            {
                throw Unprocessable("INVALID_SHARE_PERMISSION", "permission must be View or Edit.", "permission", "must be View or Edit");
            }
            var recipient = _users.FirstOrDefault(user => user.Email.Equals(NormalizeEmail(request.SharedWithEmail), StringComparison.OrdinalIgnoreCase));
            if (recipient is null || recipient.Id == userId)
            {
                throw NotFound("USER_NOT_FOUND", "The share recipient was not found.");
            }
            if (_shares.Any(share => share.VaultItemId == id && share.SharedWithUserId == recipient.Id && share.RevokedAt is null))
            {
                throw Conflict("SHARE_ALREADY_EXISTS", "An active share already exists for this user.");
            }
            var shareRecord = new ShareRecord(Guid.NewGuid(), id, userId, recipient.Id, request.Permission, DateTimeOffset.UtcNow, null);
            _shares.Add(shareRecord);
            AddAudit(userId, "VaultShared", item.ServiceName, "Success", item.Id);
            return ToShareResponse(shareRecord);
        }
    }
    public IReadOnlyList<ShareResponse> GetShares(Guid userId, Guid id)
    {
        lock (_gate)
        {
            FindVaultItem(userId, id);
            return _shares.Where(share => share.VaultItemId == id && share.RevokedAt is null)
                .Select(ToShareResponse).ToArray();
        }
    }
    public void DeleteShare(Guid userId, Guid id, Guid shareId)
    {
        lock (_gate)
        {
            FindVaultItem(userId, id);
            var share = _shares.FirstOrDefault(candidate => candidate.Id == shareId && candidate.VaultItemId == id && candidate.OwnerUserId == userId && candidate.RevokedAt is null)
                ?? throw NotFound("SHARE_NOT_FOUND", "The share was not found.");
            share.RevokedAt = DateTimeOffset.UtcNow;
        }
    }
}
