namespace Xpeak.Api.Media.Dto;

/// <summary>
/// Response of <c>POST /check_ins/{id}/media</c>. When this
/// confirmation reaches the check-in's <c>media_intent_count</c>,
/// the server flips the row to <see cref="Xpeak.Api.CheckIns.Entities.CheckInStatus.Published"/>
/// and <see cref="Publish"/> carries the XP / level / streak
/// delta so the client doesn't need an extra round-trip. When
/// more media slots remain, <see cref="Publish"/> is <c>null</c>.
/// </summary>
public sealed record ConfirmMediaResponse(
    IReadOnlyList<CheckInMediaResponse> Media,
    PublishDeltaResponse? Publish = null);

public sealed record PublishDeltaResponse(
    UserProgressionDeltaResponse User,
    StreakDeltaResponse Streak);

public sealed record UserProgressionDeltaResponse(
    Guid Id,
    int Xp,
    int Level,
    bool LeveledUp,
    int LevelsGained);

public sealed record StreakDeltaResponse(int Current, int Longest, string Unit);

public sealed record CheckInMediaResponse(
    Guid Id,
    Guid CheckInId,
    string Kind,
    string StorageKey,
    int? Width,
    int? Height,
    int? DurationSeconds,
    int Position,
    DateTimeOffset CreatedAt);
