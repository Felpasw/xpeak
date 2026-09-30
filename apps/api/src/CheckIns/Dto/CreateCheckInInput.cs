namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Input to <c>ICheckInService.CreateAsync</c>. <c>CategoryId</c> and
/// <c>Title</c> are required. <c>PerformedAt</c>, <c>DurationMinutes</c>
/// and <c>Notes</c> are optional. <c>GroupId</c> intentionally NOT
/// included — the server derives it from the loaded category to
/// keep the invariant <c>check_ins.group_id == category.group_id</c>.
/// </summary>
public sealed record CreateCheckInInput(
    Guid CategoryId,
    string Title,
    DateTimeOffset? PerformedAt = null,
    int? DurationMinutes = null,
    string? Notes = null);
