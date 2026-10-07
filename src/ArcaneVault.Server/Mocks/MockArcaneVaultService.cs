using System.Security.Cryptography;
using ArcaneVault.Server.Dtos;
using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService : IAuthService, IUserService, ICategoryService, ITagService, IVaultItemService, IAuditService, IDashboardService
{
    private const string DemoPassword = "ArcaneVault123!";
    private const int PageSizeLimit = 100;
    private static readonly string[] AllowedColors = ["pink", "blue", "green", "orange", "purple"];
    private static readonly string[] AllowedThemes = ["Light", "Dark", "System"];
    private readonly object _gate = new();
    private readonly List<UserRecord> _users = [];
    private readonly List<CategoryRecord> _categories = [];
    private readonly List<TagRecord> _tags = [];
    private readonly List<VaultRecord> _vaultItems = [];
    private readonly List<SessionRecord> _sessions = [];
    private readonly List<ShareRecord> _shares = [];
    private readonly List<AuditRecord> _auditLogs = [];
    private readonly Dictionary<Guid, Guid> _twoFactorChallenges = [];
    private readonly MockTokenStore tokens;

    public MockArcaneVaultService(MockTokenStore tokens)
    {
        this.tokens = tokens;
        Seed();
    }




































    private void Seed()
    {
        var now = DateTimeOffset.UtcNow;
        var user = CreateUser("hello@designmonk.com", "Design", "Monks", DemoPassword);
        _users.Add(user);
        var categories = new[]
        {
            new CategoryRecord(Guid.NewGuid(), user.Id, "Personal", null, "♡", "pink", now),
            new CategoryRecord(Guid.NewGuid(), user.Id, "Work", null, "▣", "blue", now),
            new CategoryRecord(Guid.NewGuid(), user.Id, "Finance", null, "◈", "green", now),
            new CategoryRecord(Guid.NewGuid(), user.Id, "Social media", null, "✣", "orange", now),
            new CategoryRecord(Guid.NewGuid(), user.Id, "Shopping", null, "◇", "pink", now),
            new CategoryRecord(Guid.NewGuid(), user.Id, "Travel", null, "⌁", "blue", now)
        };
        _categories.AddRange(categories);
        var design = new TagRecord(Guid.NewGuid(), user.Id, "Design", now);
        var browser = new TagRecord(Guid.NewGuid(), user.Id, "Browser", now);
        var login = new TagRecord(Guid.NewGuid(), user.Id, "Login", now);
        _tags.AddRange([design, browser, login]);
        var samples = new[]
        {
            ("Amazon Prime", "dark", categories[0], true, "mock-amazon"),
            ("Gmail", "light", categories[1], false, "mock-gmail"),
            ("Messenger", "blue", categories[3], true, "mock-messenger"),
            ("Udemy", "light", categories[1], true, "mock-udemy"),
            ("Netflix", "light", categories[0], false, "mock-netflix"),
            ("Coursera", "light", categories[1], true, "mock-coursera")
        };
        foreach (var (name, tone, category, favorite, password) in samples)
        {
            _vaultItems.Add(new VaultRecord(Guid.NewGuid(), user.Id, name, null, user.Email, password, null, category.Id,
                [design.Id, browser.Id, login.Id], StrengthScore(password), favorite, now, now, now, 1)
            {
                ColorTone = tone
            });
        }
        AddAudit(user.Id, "PasswordUpdated", "Netflix", "Success");
        AddAudit(user.Id, "NewSignIn", "Chrome on Windows", "Review");
        AddAudit(user.Id, "VaultShared", "Project Atlas", "Success");
        AddAudit(user.Id, "FailedSignIn", "Unknown device", "Critical");
        AddAudit(user.Id, "PasswordViewed", "Amazon Prime", "Success");
        AddAudit(user.Id, "RecoveryEmailChanged", "Account security", "Review");
    }

    private UserRecord CreateUser(string email, string firstName, string lastName, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return new UserRecord(Guid.NewGuid(), email, firstName, lastName, "Owner", DateTimeOffset.UtcNow,
            salt, HashPassword(password, salt), new SecurityRecord());
    }

    private SessionRecord CreateSession(Guid userId, string? deviceName)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new SessionRecord(Guid.NewGuid(), userId, string.IsNullOrWhiteSpace(deviceName) ? "Unknown device" : deviceName.Trim(),
            now, now, now.AddDays(30), false);
        _sessions.Add(session);
        return session;
    }

    private LoginResponse CreateLoginResponse(UserRecord user, SessionRecord session)
    {
        var issued = tokens.Issue(user.Id, session.Id);
        return new LoginResponse(issued.AccessToken, issued.RefreshToken, 900,
            new UserSummaryResponse(user.Id, user.Email, user.FirstName, user.LastName, user.Role));
    }

    private void RevokeSessionCore(Guid userId, Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(candidate => candidate.Id == sessionId && candidate.UserId == userId && !candidate.IsRevoked)
            ?? throw NotFound("SESSION_NOT_FOUND", "The session was not found.");
        session.IsRevoked = true;
        tokens.RevokeSession(userId, sessionId);
    }

    private UserRecord FindUser(Guid id) => _users.FirstOrDefault(user => user.Id == id)
        ?? throw NotFound("USER_NOT_FOUND", "The user was not found.");

    private void EnsureUser(Guid id) => _ = FindUser(id);

    private SessionRecord FindSession(Guid userId, Guid sessionId) =>
        _sessions.FirstOrDefault(session => session.Id == sessionId && session.UserId == userId && !session.IsRevoked && session.ExpiresAt > DateTimeOffset.UtcNow)
        ?? throw Unauthorized("INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");

    private CategoryRecord FindCategory(Guid userId, Guid id) =>
        _categories.FirstOrDefault(category => category.Id == id && category.UserId == userId)
        ?? throw NotFound("CATEGORY_NOT_FOUND", "The category was not found.");

    private TagRecord FindTag(Guid userId, Guid id) =>
        _tags.FirstOrDefault(tag => tag.Id == id && tag.UserId == userId)
        ?? throw NotFound("TAG_NOT_FOUND", "The tag was not found.");

    private VaultRecord FindVaultItem(Guid userId, Guid id) =>
        _vaultItems.FirstOrDefault(item => item.Id == id && item.UserId == userId)
        ?? throw NotFound("VAULT_ITEM_NOT_FOUND", "The vault item was not found.");

    private HashSet<Guid> ValidateTagIds(Guid userId, IReadOnlyList<Guid>? tagIds)
    {
        var ids = tagIds?.ToHashSet() ?? [];
        if (ids.Any(id => !_tags.Any(tag => tag.Id == id && tag.UserId == userId)))
        {
            throw NotFound("TAG_NOT_FOUND", "One or more tags were not found.");
        }
        return ids;
    }

    private void EnsureUniqueCategoryName(Guid userId, string name, Guid? exceptId)
    {
        if (_categories.Any(category => category.UserId == userId && category.Id != exceptId
            && category.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            throw Conflict("CATEGORY_NAME_EXISTS", "A category with that name already exists.");
        }
    }

    private void AddAudit(Guid? userId, string eventType, string detail, string severity, Guid? vaultItemId = null) =>
        _auditLogs.Add(new AuditRecord(Guid.NewGuid(), userId, eventType, detail, severity, DateTimeOffset.UtcNow, vaultItemId));

    private CategoryResponse ToCategoryResponse(CategoryRecord category) => new(category.Id, category.Name, category.Description,
        category.Icon, category.Color, _vaultItems.Count(item => item.UserId == category.UserId && item.CategoryId == category.Id), category.CreatedAt);

    private static TagResponse ToTagResponse(TagRecord tag) => new(tag.Id, tag.Name, tag.CreatedAt);
    private static SessionResponse ToSessionResponse(SessionRecord session) => new(session.Id, session.DeviceName, session.CreatedAt, session.LastActiveAt, session.ExpiresAt);
    private static SecuritySettingsResponse ToSettingsResponse(SecurityRecord settings) => new(settings.TwoFactorEnabled,
        settings.AutoLockEnabled, settings.AutoLockMinutes, settings.Theme, settings.SecurityRemindersEnabled, settings.UpdatedAt);
    private static UserResponse ToUserResponse(UserRecord user) => new(user.Id, user.Email, user.FirstName, user.LastName, user.Role,
        user.ProfilePhotoUrl, user.CreatedAt, user.LastLoginAt, user.Version);

    private VaultItemResponse ToVaultItemResponse(VaultRecord item) => new(item.Id, item.ServiceName, item.SiteUrl, item.Username,
        GetCategorySummary(item), GetTagSummaries(item), item.ServiceName[..Math.Min(1, item.ServiceName.Length)].ToUpperInvariant(),
        item.ColorTone, item.PasswordStrengthScore, item.IsFavorite, item.LastUsedAt, item.CreatedAt, item.UpdatedAt, item.Version);

    private VaultItemDetailResponse ToVaultItemDetail(VaultRecord item) => new(item.Id, item.ServiceName, item.SiteUrl, item.Username,
        item.Notes, GetCategorySummary(item), GetTagSummaries(item), item.ServiceName[..Math.Min(1, item.ServiceName.Length)].ToUpperInvariant(),
        item.ColorTone, item.PasswordStrengthScore, item.IsFavorite, item.LastUsedAt, item.CreatedAt, item.UpdatedAt, item.Version);

    private CategorySummaryResponse? GetCategorySummary(VaultRecord item)
    {
        var category = item.CategoryId is { } id ? _categories.FirstOrDefault(candidate => candidate.Id == id && candidate.UserId == item.UserId) : null;
        return category is null ? null : new CategorySummaryResponse(category.Id, category.Name, category.Icon, category.Color);
    }

    private IReadOnlyList<TagSummaryResponse> GetTagSummaries(VaultRecord item) => _tags
        .Where(tag => tag.UserId == item.UserId && item.TagIds.Contains(tag.Id))
        .Select(tag => new TagSummaryResponse(tag.Id, tag.Name)).ToArray();

    private ShareResponse ToShareResponse(ShareRecord share)
    {
        var recipient = FindUser(share.SharedWithUserId);
        return new ShareResponse(share.Id, share.VaultItemId, new SharedWithResponse(recipient.Id, recipient.Email), share.Permission, share.CreatedAt);
    }

    private static PageResponse<T> Paginate<T>(IReadOnlyList<T> items, int page, int pageSize)
    {
        ValidatePage(page, pageSize);
        var offset = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        return new PageResponse<T>(items.Skip(offset).Take(pageSize).ToArray(), page, pageSize, items.Count,
            (int)Math.Ceiling(items.Count / (double)pageSize));
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > PageSizeLimit)
        {
            throw BadRequest("INVALID_PAGINATION", "page must be at least 1 and pageSize must be between 1 and 100.");
        }
    }

    private static void ValidateColor(string color)
    {
        if (!AllowedColors.Contains(color, StringComparer.Ordinal))
        {
            throw Unprocessable("INVALID_CATEGORY_COLOR", "color must be pink, blue, green, orange, or purple.", "color", "must be pink, blue, green, orange, or purple");
        }
    }

    private static void CheckVersion(long current, long expected)
    {
        if (current != expected) throw Conflict("STALE_VERSION", "The resource has changed. Fetch the latest version and retry.");
    }

    private static int StrengthScore(string password)
    {
        var classes = (password.Any(char.IsLower) ? 1 : 0) + (password.Any(char.IsUpper) ? 1 : 0)
            + (password.Any(char.IsDigit) ? 1 : 0) + (password.Any(character => !char.IsLetterOrDigit(character)) ? 1 : 0);
        return Math.Clamp((password.Length >= 12 ? 1 : 0) + (password.Length >= 16 ? 1 : 0) + Math.Max(0, classes - 2), 0, 4);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    private static byte[] HashPassword(string password, byte[] salt) => Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    private static bool VerifyPassword(string password, byte[] salt, byte[] expected) =>
        CryptographicOperations.FixedTimeEquals(HashPassword(password, salt), expected);

    private static ApiException BadRequest(string code, string message) => new(StatusCodes.Status400BadRequest, code, message);
    private static ApiException Unauthorized(string code, string message) => new(StatusCodes.Status401Unauthorized, code, message);
    private static ApiException NotFound(string code, string message) => new(StatusCodes.Status404NotFound, code, message);
    private static ApiException Conflict(string code, string message) => new(StatusCodes.Status409Conflict, code, message);
    private static ApiException Unprocessable(string code, string message, string field, string issue) =>
        new(StatusCodes.Status422UnprocessableEntity, code, message, [new ApiErrorDetail(field, issue)]);

    private sealed class UserRecord(Guid id, string email, string firstName, string lastName, string role, DateTimeOffset createdAt,
        byte[] passwordSalt, byte[] passwordHash, SecurityRecord security)
    {
        public Guid Id { get; } = id;
        public string Email { get; set; } = email;
        public string FirstName { get; set; } = firstName;
        public string LastName { get; set; } = lastName;
        public string Role { get; } = role;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset? LastLoginAt { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public long Version { get; set; } = 1;
        public byte[] PasswordSalt { get; } = passwordSalt;
        public byte[] PasswordHash { get; } = passwordHash;
        public SecurityRecord Security { get; } = security;
    }

    private sealed class SecurityRecord
    {
        public bool TwoFactorEnabled { get; set; }
        public bool AutoLockEnabled { get; set; } = true;
        public int AutoLockMinutes { get; set; } = 15;
        public string Theme { get; set; } = "Light";
        public bool SecurityRemindersEnabled { get; set; } = true;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class CategoryRecord(Guid id, Guid userId, string name, string? description, string icon, string color, DateTimeOffset createdAt)
    {
        public Guid Id { get; } = id;
        public Guid UserId { get; } = userId;
        public string Name { get; set; } = name;
        public string? Description { get; set; } = description;
        public string Icon { get; set; } = icon;
        public string Color { get; set; } = color;
        public DateTimeOffset CreatedAt { get; } = createdAt;
    }

    private sealed record TagRecord(Guid Id, Guid UserId, string Name, DateTimeOffset CreatedAt);

    private sealed class VaultRecord(Guid id, Guid userId, string serviceName, string? siteUrl, string username, string password,
        string? notes, Guid? categoryId, HashSet<Guid> tagIds, int passwordStrengthScore, bool isFavorite, DateTimeOffset? lastUsedAt,
        DateTimeOffset createdAt, DateTimeOffset updatedAt, long version)
    {
        public Guid Id { get; } = id;
        public Guid UserId { get; } = userId;
        public string ServiceName { get; set; } = serviceName;
        public string? SiteUrl { get; set; } = siteUrl;
        public string Username { get; set; } = username;
        public string Password { get; set; } = password;
        public string? Notes { get; set; } = notes;
        public Guid? CategoryId { get; set; } = categoryId;
        public HashSet<Guid> TagIds { get; set; } = tagIds;
        public int PasswordStrengthScore { get; set; } = passwordStrengthScore;
        public bool IsFavorite { get; set; } = isFavorite;
        public DateTimeOffset? LastUsedAt { get; set; } = lastUsedAt;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset UpdatedAt { get; set; } = updatedAt;
        public long Version { get; set; } = version;
        public string ColorTone { get; set; } = "dark";
        public List<PasswordHistoryResponse> History { get; } = [];
    }

    private sealed class SessionRecord(Guid id, Guid userId, string? deviceName, DateTimeOffset createdAt,
        DateTimeOffset lastActiveAt, DateTimeOffset expiresAt, bool isRevoked)
    {
        public Guid Id { get; } = id;
        public Guid UserId { get; } = userId;
        public string? DeviceName { get; } = deviceName;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset LastActiveAt { get; set; } = lastActiveAt;
        public DateTimeOffset ExpiresAt { get; } = expiresAt;
        public bool IsRevoked { get; set; } = isRevoked;
    }

    private sealed class ShareRecord(Guid id, Guid vaultItemId, Guid ownerUserId, Guid sharedWithUserId,
        string permission, DateTimeOffset createdAt, DateTimeOffset? revokedAt)
    {
        public Guid Id { get; } = id;
        public Guid VaultItemId { get; } = vaultItemId;
        public Guid OwnerUserId { get; } = ownerUserId;
        public Guid SharedWithUserId { get; } = sharedWithUserId;
        public string Permission { get; } = permission;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public DateTimeOffset? RevokedAt { get; set; } = revokedAt;
    }

    private sealed record AuditRecord(Guid Id, Guid? UserId, string EventType, string Detail, string Severity, DateTimeOffset CreatedAt, Guid? VaultItemId);
}
