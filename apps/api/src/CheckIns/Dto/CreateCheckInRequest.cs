namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// HTTP request payload for <c>POST /check_ins</c>. <c>CategoryId</c>
/// and <c>Title</c> are required. <c>PerformedAt</c> is optional —
/// when omitted the server stamps <c>now()</c>. When provided it may
/// backfill up to 7 days into the past (never the future). <c>GroupId</c>
/// is intentionally NOT accepted — the server derives it from the
/// loaded category.
/// </summary>
public sealed record CreateCheckInRequest(
    Guid CategoryId,
    string Title,
    DateTimeOffset? PerformedAt = null,
    int? DurationMinutes = null,
    string? Notes = null);
