using ArcaneVault.Server.Dtos;

namespace ArcaneVault.Server.Services;

public interface IUserService
{
    UserResponse GetUser(Guid userId);
    UserResponse UpdateUser(Guid userId, UpdateUserRequest request, long expectedVersion);
    SecuritySettingsResponse GetSecuritySettings(Guid userId);
    SecuritySettingsResponse UpdateSecuritySettings(Guid userId, UpdateSecuritySettingsRequest request);
    UserResponse UpdateProfilePhoto(Guid userId, string contentType, byte[] photo, long expectedVersion);
    UserProfilePhotoResponse? GetProfilePhoto(Guid userId);
    void DeleteProfilePhoto(Guid userId);
}
