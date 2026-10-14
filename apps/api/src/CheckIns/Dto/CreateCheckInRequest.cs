namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// HTTP request payload for <c>POST /check_ins</c>. <c>Title</c> is
/// required; everything else is optional. <c>PerformedAt</c> defaults
/// to <c>now()</c> and may backfill up to 7 days.
/// </summary>
public sealed record CreateCheckInRequest(
    string Title,
    DateTimeOffset? PerformedAt = null,
    int? DurationMinutes = null,
    string? Notes = null,
    bool WithMedia = false);
