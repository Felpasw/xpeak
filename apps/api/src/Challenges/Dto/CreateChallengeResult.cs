using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Return of <c>IChallengeService.CreateAsync</c>. Carries
/// the persisted <see cref="Challenge"/>, the owner's auto-inserted
/// <see cref="ChallengeMembership"/>, the default share-link
/// <see cref="ChallengeInvitation"/> that lets the owner invite
/// others without a separate request, and the owner-declared
/// progression <see cref="Tiers"/> sorted by
/// <see cref="ChallengeTier.MinLevel"/> ascending (empty when the
/// owner skipped the ladder). The share-link's
/// <see cref="ChallengeInvitation.Id"/> is the public share token.</summary>
public sealed record CreateChallengeResult(
    Challenge Challenge,
    ChallengeMembership OwnerMembership,
    ChallengeInvitation ShareLink,
    IReadOnlyList<ChallengeTier> Tiers);
