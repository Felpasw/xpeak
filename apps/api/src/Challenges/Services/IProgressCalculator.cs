using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Services;

public interface IProgressCalculator
{
    /// <summary>
    /// Computes the current <see cref="MembershipProgress"/> for a
    /// given membership: sums the member's check-ins that live in
    /// the challenge's category whitelist and fall inside
    /// <c>[starts_at, ends_at]</c>, converts that XP to the
    /// equivalent level via <c>LevelCurve.LevelForXp</c>, and
    /// compares it to the challenge's <c>target_level</c>.
    /// Pure read — never mutates the membership row. The caller
    /// decides when to persist the returned snapshot.
    /// </summary>
    Task<MembershipProgress> ComputeAsync(
        Guid membershipId,
        CancellationToken ct = default);
}
