namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Input to <c>ICheckInService.CreateAsync</c>. <c>CategoryId</c> is
/// required; <c>DurationMinutes</c> and <c>Notes</c> are optional
/// (nullable both here and on the persisted row). <c>GroupId</c>
/// intentionally NOT included — the server derives it from the
/// loaded category to keep the invariant
/// <c>check_ins.group_id == category.group_id</c>.
/// </summary>
public sealed record CreateCheckInInput(
    Guid CategoryId,
    int? DurationMinutes = null,
    string? Notes = null);
