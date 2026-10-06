using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Media.Entities;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// A single row of a cursor-paginated listing, with
/// <see cref="HasMedia"/> projected via <c>EXISTS</c> against
/// <c>check_in_media</c>, <see cref="Category"/> projected via
/// a JOIN against <c>categories</c>, and <see cref="FirstMedia"/>
/// projected as the position-ordered head of the attachments —
/// so the endpoint can render a card without any second round-trip.
/// </summary>
public sealed record CheckInListItem(
    CheckIn CheckIn,
    bool HasMedia,
    CategorySnapshot Category,
    FirstMediaSource? FirstMedia);

/// <summary>
/// Raw head of the attachment list for a check-in. The endpoint
/// turns this into a <c>MediaPreview</c> by asking the configured
/// <c>IMediaStorage</c> for a thumbnail URL — the repository stays
/// backend-agnostic.
/// </summary>
public sealed record FirstMediaSource(MediaKind Kind, string StorageKey);

/// <summary>
/// One page of a cursor-paginated check-in listing.
/// <see cref="NextCursor"/> is <c>null</c> on the last page.
/// </summary>
public sealed record CheckInPage(
    IReadOnlyList<CheckInListItem> Items,
    string? NextCursor);
