namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Input to <c>ICheckInService.CreateAsync</c>. <c>Title</c> is required;
/// everything else is optional. Phase 30 removed the per-category scope,
/// so no <c>CategoryId</c> or <c>GroupId</c> ride on the input.
/// </summary>
public sealed record CreateCheckInInput(
    string Title,
    DateTimeOffset? PerformedAt = null,
    int? DurationMinutes = null,
    string? Notes = null,
    bool WithMedia = false);
