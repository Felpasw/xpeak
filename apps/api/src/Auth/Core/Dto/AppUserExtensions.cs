using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

/// <summary>
/// Mapping from the persistence entity <see cref="AppUser"/> to the
/// public <see cref="UserResponse"/> DTO.
/// </summary>
internal static class AppUserExtensions
{
    public static UserResponse ToResponse(this AppUser u) => new(
        Id: u.Id,
        Username: u.UserName ?? string.Empty,
        Email: u.Email ?? string.Empty,
        AvatarUrl: u.AvatarUrl,
        Level: u.Level,
        Xp: u.Xp,
        CreatedAt: u.CreatedAt);
}
