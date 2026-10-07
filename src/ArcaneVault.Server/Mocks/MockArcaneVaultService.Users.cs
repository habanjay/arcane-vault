using ArcaneVault.Server.Dtos;

using ArcaneVault.Server.Services;

namespace ArcaneVault.Server.Mocks;

public sealed partial class MockArcaneVaultService
{
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
}
