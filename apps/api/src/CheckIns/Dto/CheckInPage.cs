using Xpeak.Api.CheckIns.Entities;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// A single row of a cursor-paginated listing, with
/// <see cref="HasMedia"/> projected via <c>EXISTS</c> against
/// <c>check_in_media</c> so the endpoint can render without a
/// second round-trip.
/// </summary>
public sealed record CheckInListItem(CheckIn CheckIn, bool HasMedia);

/// <summary>
/// One page of a cursor-paginated check-in listing.
/// <see cref="NextCursor"/> is <c>null</c> on the last page.
/// </summary>
public sealed record CheckInPage(
    IReadOnlyList<CheckInListItem> Items,
    string? NextCursor);
