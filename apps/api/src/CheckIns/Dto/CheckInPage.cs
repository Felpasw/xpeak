using Xpeak.Api.CheckIns.Entities;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// One page of a cursor-paginated check-in listing.
/// <see cref="NextCursor"/> is <c>null</c> on the last page.
/// </summary>
public sealed record CheckInPage(
    IReadOnlyList<CheckIn> Items,
    string? NextCursor);
