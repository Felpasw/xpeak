using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.Users;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>A single row of the challenge activity feed: the
/// check-in (same shape <c>/check_ins</c> returns) paired with the
/// public card of its author so the mobile can link into
/// <c>/u?username=…</c> without a second round-trip.</summary>
public sealed record ChallengeCheckInFeedEntry(
    CheckInResponse CheckIn,
    PublicUserResponse Author);

public sealed record ListChallengeCheckInsResponse(
    IReadOnlyList<ChallengeCheckInFeedEntry> Items,
    string? NextCursor);
