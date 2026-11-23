namespace Xpeak.Api.CheckIns.Dto;

/// <summary>Author snapshot embedded in the detail / list responses
/// so the mobile card can render a header without a second
/// round-trip. Mirrors the public projection of <c>AppUser</c>
/// fields that are safe to hand to any authenticated viewer.</summary>
public sealed record CheckInAuthorDto(
    Guid Id,
    string Username,
    string? AvatarUrl);

/// <summary>Reactions summary attached to each feed row and to the
/// detail response. <c>Counts</c> is grouped by emoji; <c>Mine</c>
/// is the caller's own emojis on this check-in.</summary>
public sealed record CheckInReactionsSummary(
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<string> Mine);

/// <summary>Payload of <c>GET /check_ins/{id}</c>. Carries the same
/// base fields as <see cref="CheckInResponse"/> plus the author
/// snapshot, the reactions aggregate and the live comment count.
/// The detail route uses this to render the full Instagram-style
/// publication view without secondary requests.</summary>
public sealed record CheckInDetailResponse(
    Guid Id,
    string Title,
    int XpEarned,
    ScoringSnapshot ScoringSnapshot,
    DateTimeOffset PerformedAt,
    int? DurationMinutes,
    string? Notes,
    bool HasMedia,
    MediaPreview? MediaPreview,
    CheckInAuthorDto Author,
    CheckInReactionsSummary Reactions,
    int CommentsCount);
