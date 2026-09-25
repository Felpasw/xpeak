using Xpeak.Api.CheckIns.Entities;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Wire representation of a single check-in row. Used both as the
/// standalone item in <c>ListCheckInsResponse.CheckIns</c> and inside
/// the create response.
/// </summary>
public sealed record CheckInResponse(
    Guid Id,
    Guid CategoryId,
    Guid GroupId,
    int XpEarned,
    ScoringSnapshot ScoringSnapshot,
    DateTimeOffset PerformedAt,
    int? DurationMinutes,
    string? Notes)
{
    public static CheckInResponse From(CheckIn c) => new(
        c.Id,
        c.CategoryId,
        c.GroupId,
        c.XpEarned,
        c.ScoringSnapshot,
        c.PerformedAt,
        c.DurationMinutes,
        c.Notes);
}

/// <summary>User progression block returned inside the create response.</summary>
public sealed record UserProgressionResponse(
    Guid Id,
    int Xp,
    int Level,
    bool LeveledUp,
    int LevelsGained);

/// <summary>Per-group streak block returned inside the create response.</summary>
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
