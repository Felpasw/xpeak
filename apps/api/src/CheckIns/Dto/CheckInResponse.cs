using Xpeak.Api.CheckIns.Entities;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Wire representation of a single check-in row. Used both as the
/// standalone item in <c>ListCheckInsResponse.CheckIns</c> and inside
/// the create response.
/// </summary>
public sealed record CheckInResponse(
    Guid Id,
    string Title,
    int XpEarned,
    ScoringSnapshot ScoringSnapshot,
    DateTimeOffset PerformedAt,
    int? DurationMinutes,
    string? Notes,
    bool HasMedia,
    MediaPreview? MediaPreview)
{
    public static CheckInResponse From(
        CheckIn c,
        MediaPreview? mediaPreview = null,
        bool hasMedia = false) => new(
        c.Id,
        c.Title,
        c.XpEarned,
        c.ScoringSnapshot,
        c.PerformedAt,
        c.DurationMinutes,
        c.Notes,
        hasMedia,
        mediaPreview);
}

/// <summary>
/// Thumbnail hint for the first attachment of a check-in, so a feed
/// card can render a preview without a second fetch. <c>Kind</c> is
/// the lowercase string of <see cref="MediaKind"/> (<c>"photo"</c> or
/// <c>"video"</c>). <c>ThumbUrl</c> is a backend-computed URL (see
/// <c>IMediaStorage.BuildThumbUrl</c>).
/// </summary>
public sealed record MediaPreview(string Kind, string ThumbUrl);

/// <summary>User progression block returned inside the create response.</summary>
public sealed record UserProgressionResponse(
    Guid Id,
    int Xp,
    int Level,
    bool LeveledUp,
    int LevelsGained);

/// <summary>Per-user streak block returned inside the create response.</summary>
public sealed record StreakResponse(
    int Current,
    int Longest,
    string Unit);

/// <summary>Payload of <c>POST /check_ins</c>.</summary>
public sealed record CreateCheckInResponse(
    CheckInResponse CheckIn,
    UserProgressionResponse User,
    StreakResponse Streak);

/// <summary>Payload of <c>GET /check_ins</c>.</summary>
public sealed record ListCheckInsResponse(
    IReadOnlyList<CheckInResponse> CheckIns,
    string? NextCursor);
