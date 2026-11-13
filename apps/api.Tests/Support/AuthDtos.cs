namespace Xpeak.Api.Tests.Support;

public sealed record UserDto(
    Guid Id,
    string Username,
    string Email,
    string? AvatarUrl,
    int Level,
    int Xp,
    int CurrentStreakDays,
    int LongestStreakDays,
    string? Background,
    DateTimeOffset CreatedAt,
    LevelProgressDto LevelProgress);

public sealed record LevelProgressDto(
    int XpIntoLevel,
    int XpToNextLevel,
    double Percent);

public sealed record AuthResponseDto(UserDto User);
