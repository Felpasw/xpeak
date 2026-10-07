using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Dto;

/// <summary>Return of <c>IChallengeService.CreateAsync</c>. Carries
/// the persisted <see cref="Challenge"/>, the owner's auto-inserted
/// <see cref="ChallengeMembership"/> and the <see cref="ChallengeCategory"/>
/// join rows that scope which categories count toward the win.</summary>
public sealed record CreateChallengeResult(
    Challenge Challenge,
    ChallengeMembership OwnerMembership,
    IReadOnlyList<ChallengeCategory> Categories);
