using Xpeak.Api.Users;
using Xpeak.Api.Xp;

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
        CurrentStreakDays: 0,
        LongestStreakDays: 0,
        Background: u.Background,
        CreatedAt: u.CreatedAt,
        LevelProgress: BuildLevelProgress(u.Level, u.Xp));

    private static LevelProgressResponse BuildLevelProgress(int level, int xp)
    {
        var floor = LevelCurve.XpForLevel(level);
        var ceiling = LevelCurve.XpForLevel(level + 1);
        var span = ceiling - floor;
        if (span <= 0)
        {
            return new LevelProgressResponse(0, 0, 1.0);
        }

        var into = Math.Clamp(xp - floor, 0, span);
        var percent = (double)into / span;
        return new LevelProgressResponse(into, span, percent);
    }
}
