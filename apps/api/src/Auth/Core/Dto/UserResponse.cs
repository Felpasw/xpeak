using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Core;

public sealed record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string? AvatarUrl,
    int Level,
    int Xp,
    int CurrentStreakDays,
    int LongestStreakDays,
    Backgrounds? Background,
    DateTimeOffset CreatedAt,
    LevelProgressResponse LevelProgress);

/// <summary>
/// Wire shape of the user's progression inside their current global
/// level. <c>XpToNextLevel</c> is the total XP span of the current
/// level (<see cref="Xp.LevelCurve.XpForLevel(int)"/> at <c>level+1</c>
/// minus at <c>level</c>). <c>XpIntoLevel</c> is the user's XP minus
/// the floor of the current level. <c>Percent</c> is a 0..1 ratio.
/// At the (practical) ceiling both counters sit at 0 and <c>Percent</c>
/// stays 1.
/// </summary>
public sealed record LevelProgressResponse(
    int XpIntoLevel,
    int XpToNextLevel,
    double Percent);
