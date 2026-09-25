namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// HTTP request payload for <c>POST /check_ins</c>. Only
/// <c>CategoryId</c> is required. <c>GroupId</c> is intentionally
/// NOT accepted — the server derives it from the loaded category.
/// </summary>
public sealed record CreateCheckInRequest(
    Guid CategoryId,
    int? DurationMinutes = null,
    string? Notes = null);
