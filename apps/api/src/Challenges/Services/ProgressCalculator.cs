using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Challenges.Services;

public sealed class ProgressCalculator(
    AppDbContext db,
    IProgressionService progression,
    TimeProvider time) : IProgressCalculator
{
    public async Task<MembershipProgress> ComputeAsync(
        Guid membershipId,
        CancellationToken ct = default)
    {
        var membership = await db.ChallengeMemberships
            .AsNoTracking()
            .SingleAsync(m => m.Id == membershipId, ct);

        var challenge = await db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.Id == membership.ChallengeId, ct);

        // No category whitelist — every check-in the member posts
        // inside the window contributes.
        var totalXp = await db.CheckIns
            .AsNoTracking()
            .Where(ci => ci.UserId == membership.UserId
                && ci.PerformedAt >= challenge.StartsAt
                && ci.PerformedAt <= challenge.EndsAt)
            .SumAsync(ci => (int?)ci.XpEarned, ct) ?? 0;

        var requiredXp = progression.XpForLevel(challenge.TargetLevel);
        var achievedAt = totalXp >= requiredXp
            ? (DateTimeOffset?)time.GetUtcNow()
            : null;

        return new MembershipProgress(totalXp, requiredXp, achievedAt);
    }
}
