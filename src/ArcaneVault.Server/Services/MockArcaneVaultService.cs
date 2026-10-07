using System.Security.Cryptography;
using ArcaneVault.Server.Apis;
using ArcaneVault.Server.Infrastructure;

namespace ArcaneVault.Server.Services;

public sealed class MockArcaneVaultService : IArcaneVaultService
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

    public UserResponse Register(RegisterRequest request)
    {
        lock (_gate)
        {
            var email = NormalizeEmail(request.Email);
            if (_users.Any(user => user.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
            {
                throw Conflict("EMAIL_ALREADY_EXISTS", "An account with that email already exists.");
            }

            var user = CreateUser(email, request.FirstName.Trim(), request.LastName.Trim(), request.MasterPassword);
            _users.Add(user);
            return ToUserResponse(user);
        }
    }

    public (LoginResponse? Response, TwoFactorChallengeResponse? Challenge) Login(LoginRequest request, string? deviceName)
    {
        lock (_gate)
        {
            var email = NormalizeEmail(request.Email);
            var user = _users.FirstOrDefault(candidate => candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
            if (user is null || !VerifyPassword(request.MasterPassword, user.PasswordSalt, user.PasswordHash))
            {
                AddAudit(user?.Id, "FailedSignIn", "Unknown device", "Critical");
                throw Unauthorized("INVALID_CREDENTIALS", "The email or master password is incorrect.");
            }

            if (user.Security.TwoFactorEnabled)
            {
                var challengeId = Guid.NewGuid();
                _twoFactorChallenges[challengeId] = user.Id;
                return (null, new TwoFactorChallengeResponse(true, challengeId));
            }

            user.LastLoginAt = DateTimeOffset.UtcNow;
            var session = CreateSession(user.Id, deviceName);
            AddAudit(user.Id, "NewSignIn", session.DeviceName ?? "Unknown device", "Review");
            return (CreateLoginResponse(user, session), null);
        }
    }

    public LoginResponse Refresh(RefreshRequest request)
    {
        lock (_gate)
        {
            if (!tokens.TryRotateRefreshToken(request.RefreshToken, out var userId, out var sessionId))
            {
                throw Unauthorized("INVALID_REFRESH_TOKEN", "The refresh token is invalid or expired.");
            }

            var session = FindSession(userId, sessionId);
            var user = FindUser(userId);
            session.LastActiveAt = DateTimeOffset.UtcNow;
            return CreateLoginResponse(user, session);
        }
    }

    public LoginResponse VerifyTwoFactor(TwoFactorVerifyRequest request)
    {
        lock (_gate)
        {
            if (!_twoFactorChallenges.Remove(request.ChallengeId, out var userId))
            {
                throw Unauthorized("INVALID_2FA_CODE", "The verification code is invalid.");
            }

            if (request.Code != "123456")
            {
                AddAudit(userId, "FailedSignIn", "Two-factor verification failed", "Critical");
                throw Unauthorized("INVALID_2FA_CODE", "The verification code is invalid.");
            }

            var user = FindUser(userId);
            user.LastLoginAt = DateTimeOffset.UtcNow;
            var session = CreateSession(userId, "Verified device");
            AddAudit(userId, "NewSignIn", session.DeviceName!, "Review");
            return CreateLoginResponse(user, session);
        }
    }

    public void Logout(Guid userId, Guid sessionId)
    {
        lock (_gate)
        {
            RevokeSessionCore(userId, sessionId);
        }
    }

    public IReadOnlyList<SessionResponse> GetSessions(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            return _sessions.Where(session => session.UserId == userId && !session.IsRevoked && session.ExpiresAt > DateTimeOffset.UtcNow)
                .OrderByDescending(session => session.LastActiveAt)
                .Select(ToSessionResponse)
                .ToArray();
        }
    }

    public void RevokeSession(Guid userId, Guid sessionId)
    {
        lock (_gate)
        {
            RevokeSessionCore(userId, sessionId);
        }
    }

    public UserResponse GetUser(Guid userId)
    {
        lock (_gate)
        {
            return ToUserResponse(FindUser(userId));
        }
    }

    public UserResponse UpdateUser(Guid userId, UpdateUserRequest request, long expectedVersion)
    {
        lock (_gate)
        {
            var user = FindUser(userId);
            CheckVersion(user.Version, expectedVersion);
            if (request.Email is not null)
            {
                var email = NormalizeEmail(request.Email);
                if (_users.Any(candidate => candidate.Id != userId && candidate.Email.Equals(email, StringComparison.OrdinalIgnoreCase)))
                {
                    throw Conflict("EMAIL_ALREADY_EXISTS", "An account with that email already exists.");
                }
                user.Email = email;
            }
            if (request.FirstName is not null) user.FirstName = request.FirstName.Trim();
            if (request.LastName is not null) user.LastName = request.LastName.Trim();
            user.Version++;
            return ToUserResponse(user);
        }
    }

    public SecuritySettingsResponse GetSecuritySettings(Guid userId)
    {
        lock (_gate)
        {
            return ToSettingsResponse(FindUser(userId).Security);
        }
    }

    public SecuritySettingsResponse UpdateSecuritySettings(Guid userId, UpdateSecuritySettingsRequest request)
    {
        lock (_gate)
        {
            var settings = FindUser(userId).Security;
            if (request.AutoLockMinutes is < 1 or > 120)
            {
                throw Unprocessable("INVALID_AUTO_LOCK_MINUTES", "autoLockMinutes must be between 1 and 120.", "autoLockMinutes", "must be between 1 and 120");
            }
            if (request.Theme is not null && !AllowedThemes.Contains(request.Theme, StringComparer.Ordinal))
            {
                throw Unprocessable("INVALID_THEME", "theme must be Light, Dark, or System.", "theme", "must be Light, Dark, or System");
            }
            if (request.TwoFactorEnabled is { } twoFactorEnabled) settings.TwoFactorEnabled = twoFactorEnabled;
            if (request.AutoLockEnabled is { } autoLockEnabled) settings.AutoLockEnabled = autoLockEnabled;
            if (request.AutoLockMinutes is { } autoLockMinutes) settings.AutoLockMinutes = autoLockMinutes;
            if (request.Theme is not null) settings.Theme = request.Theme;
            if (request.SecurityRemindersEnabled is { } reminders) settings.SecurityRemindersEnabled = reminders;
            settings.UpdatedAt = DateTimeOffset.UtcNow;
            return ToSettingsResponse(settings);
        }
    }

    public UserResponse UpdateProfilePhoto(Guid userId, string contentType, long expectedVersion)
    {
        lock (_gate)
        {
            var user = FindUser(userId);
            CheckVersion(user.Version, expectedVersion);
            user.ProfilePhotoUrl = $"mock://profile/{user.Id:N}";
            user.Version++;
            return ToUserResponse(user);
        }
    }

    public void DeleteProfilePhoto(Guid userId)
    {
        lock (_gate)
        {
            var user = FindUser(userId);
            user.ProfilePhotoUrl = null;
            user.Version++;
        }
    }

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

    public PageResponse<AuditLogResponse> GetAuditLogs(Guid userId, string? severity, string? eventType, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            if (from is { } startDate && to is { } endDate && startDate > endDate)
            {
                throw BadRequest("INVALID_DATE_RANGE", "from must be earlier than or equal to to.");
            }
            IEnumerable<AuditRecord> query = _auditLogs.Where(entry => entry.UserId == userId);
            if (severity is not null)
            {
                if (severity is not ("Success" or "Review" or "Critical")) throw BadRequest("INVALID_SEVERITY", "severity must be Success, Review, or Critical.");
                query = query.Where(entry => entry.Severity == severity);
            }
            if (eventType is not null) query = query.Where(entry => entry.EventType.Equals(eventType, StringComparison.OrdinalIgnoreCase));
            if (from is { } start) query = query.Where(entry => entry.CreatedAt >= start);
            if (to is { } end) query = query.Where(entry => entry.CreatedAt <= end);
            return Paginate(query.OrderByDescending(entry => entry.CreatedAt).Select(entry =>
                new AuditLogResponse(entry.Id, entry.EventType, entry.Detail, entry.Severity, entry.CreatedAt)).ToArray(), page, pageSize);
        }
    }

    public AuditSummaryResponse GetAuditSummary(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            var month = DateTimeOffset.UtcNow;
            var events = _auditLogs.Where(entry => entry.UserId == userId && entry.CreatedAt.Year == month.Year && entry.CreatedAt.Month == month.Month).ToArray();
            return new AuditSummaryResponse(events.Length, events.Count(entry => entry.Severity == "Success"),
                events.Count(entry => entry.Severity == "Review"), events.Count(entry => entry.Severity == "Critical"));
        }
    }

    public DashboardSummaryResponse GetDashboardSummary(Guid userId)
    {
        lock (_gate)
        {
            EnsureUser(userId);
            var items = _vaultItems.Where(item => item.UserId == userId).ToArray();
            var categories = _categories.Where(category => category.UserId == userId).ToArray();
            return new DashboardSummaryResponse(
                [
                    new DashboardStatResponse("Total passwords", items.Length),
                    new DashboardStatResponse("Strong passwords", items.Count(item => item.PasswordStrengthScore >= 3)),
                    new DashboardStatResponse("Need attention", items.Count(item => item.PasswordStrengthScore < 2))
                ],
                items.OrderByDescending(item => item.UpdatedAt).Take(5)
                    .Select(item => new RecentVaultItemResponse(item.Id, item.ServiceName, item.Username, item.IsFavorite, item.ColorTone, item.UpdatedAt)).ToArray(),
                categories.Select(category => new DashboardCategoryResponse(category.Id, category.Name, category.Icon,
                    items.Count(item => item.CategoryId == category.Id))).ToArray());
        }
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
