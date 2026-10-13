using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Return of <c>IChallengeService.CreateAsync</c>. Carries
/// the persisted <see cref="Challenge"/> and the owner's
/// auto-inserted <see cref="ChallengeMembership"/>.</summary>
public sealed record CreateChallengeResult(
    Challenge Challenge,
    ChallengeMembership OwnerMembership);
