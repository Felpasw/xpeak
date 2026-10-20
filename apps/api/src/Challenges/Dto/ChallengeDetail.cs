using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Internal detail shape returned by
/// <c>IChallengeService.ListForUserAsync</c> and
/// <c>GetForUserAsync</c>. Carries the challenge, the caller's
/// membership when present, the challenge's share-link invitation
/// when the caller is the owner (null otherwise), the ordered
/// list of progression tiers (empty when none) and the member's
/// current tier (null when there is no membership, no tiers, or
/// the progress is below the first floor). Endpoints turn this
/// into their wire DTO.</summary>
public sealed record ChallengeDetail(
    Challenge Challenge,
    ChallengeMembership? Membership,
    ChallengeInvitation? ShareLink = null,
    IReadOnlyList<ChallengeTier>? Tiers = null,
    ChallengeTier? CurrentTier = null);
