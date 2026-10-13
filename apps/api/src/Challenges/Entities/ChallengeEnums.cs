namespace Xpeak.Api.Challenges.Entities;

/// <summary>Whether the challenge is listable by non-members.
/// Private challenges only surface to the owner and invitees.</summary>
public enum ChallengeVisibility
{
    Public,
    Private,
}

/// <summary>Lifecycle position. The Hangfire job (T-015-07) owns
/// the transitions: Active → Completed when the target_level is
/// hit, Active → Failed when ends_at passes without hitting it.
/// There is no "upcoming" or "cancelled" state — challenges start
/// Active on creation, and owners who want out delete the row.</summary>
public enum ChallengeState
{
    Active,
    Completed,
    Failed,
}

/// <summary>Role of a user inside a <c>challenge_memberships</c> row.
/// Owner is the creator (edit, cancel, invite); everyone else is a
/// member.</summary>
public enum ChallengeMembershipRole
{
    Owner,
    Member,
}

/// <summary>Terminal outcome written on the membership when the
/// challenge finishes for that user. Symmetric with ChallengeState
/// (no cancelled — delete the challenge instead).</summary>
public enum ChallengeMembershipResult
{
    Completed,
    Failed,
}
