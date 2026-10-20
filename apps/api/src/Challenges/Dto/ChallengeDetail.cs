using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Internal detail shape returned by
/// <c>IChallengeService.ListForUserAsync</c> and
/// <c>GetForUserAsync</c>. Carries the challenge, the caller's
/// membership when present, and the challenge's share-link
/// invitation when the caller is the owner (null otherwise).
/// Endpoints turn this into their wire DTO.</summary>
public sealed record ChallengeDetail(
    Challenge Challenge,
    ChallengeMembership? Membership,
    ChallengeInvitation? ShareLink = null);
