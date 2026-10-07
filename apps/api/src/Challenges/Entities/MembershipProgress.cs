namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// Cached per-member progress snapshot, persisted as JSONB on
/// <c>challenge_memberships.progress</c>. Authoritative source is
/// always the <c>IProgressCalculator</c> re-computed on read; this
/// cache exists so the Hangfire job (T-015-07) can scan membership
/// rows cheaply and the mobile card can render without a second
/// round-trip. <see cref="AchievedAt"/> is set when the user hits
/// the target, otherwise null.
/// </summary>
public sealed record MembershipProgress(
    int Value,
    int Target,
    DateTimeOffset? AchievedAt)
{
    public static MembershipProgress Empty(int target) => new(0, target, null);
}
