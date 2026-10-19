using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Return of <c>IChallengeService.JoinWithInvitationAsync</c>.
/// Carries the <see cref="Challenge"/> the caller joined and the
/// freshly-inserted <see cref="ChallengeMembership"/>.</summary>
public sealed record JoinChallengeResult(
    Challenge Challenge,
    ChallengeMembership Membership);
