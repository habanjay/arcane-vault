using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IUserService
{
    UserResponse GetUser(Guid userId);
    UserResponse UpdateUser(Guid userId, UpdateUserRequest request, long expectedVersion);
    SecuritySettingsResponse GetSecuritySettings(Guid userId);
    SecuritySettingsResponse UpdateSecuritySettings(Guid userId, UpdateSecuritySettingsRequest request);
    UserResponse UpdateProfilePhoto(Guid userId, string contentType, long expectedVersion);
    void DeleteProfilePhoto(Guid userId);
}
