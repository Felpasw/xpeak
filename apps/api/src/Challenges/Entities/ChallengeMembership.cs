namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// Join row between a user and a challenge. The owner has a row of
/// <see cref="ChallengeMembershipRole.Owner"/> inserted at
/// challenge creation; everyone else joins through the (future)
/// invite flow and arrives as <see cref="ChallengeMembershipRole.Member"/>.
/// <see cref="LeftAt"/> is populated when the user leaves voluntarily;
/// <see cref="Result"/> is populated when the challenge finishes
/// for that user (completed, failed or cancelled).
/// </summary>
public sealed class ChallengeMembership
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public Guid UserId { get; set; }

    public ChallengeMembershipRole Role { get; set; } = ChallengeMembershipRole.Member;

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LeftAt { get; set; }

    public MembershipProgress Progress { get; set; } = MembershipProgress.Empty(0);

    public ChallengeMembershipResult? Result { get; set; }
}
