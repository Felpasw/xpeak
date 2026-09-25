using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.CheckIns.Services;

public sealed class StreakService(
    AppDbContext db,
    IGroupConfigService configs) : IStreakService
{
    public async Task<StreakInfo> ComputeAsync(
        Guid userId,
        Guid groupId,
        DateOnly asOf,
        CancellationToken ct = default)
    {
        var config = await configs.GetStreakConfigAsync(groupId, ct);

        // Pull raw `performed_at` timestamps; convert to DateOnly in
        // memory so we stay off any Postgres-side date functions and
        // keep the query dead simple. Bounded by the
        // (user_id, group_id, performed_at) index — cheap even at
        // years of use.
        var timestamps = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.GroupId == groupId)
            .Select(c => c.PerformedAt)
            .ToListAsync(ct);

        var distinctDates = timestamps
            .Select(t => DateOnly.FromDateTime(t.UtcDateTime))
            .Distinct()
            .ToList();

        return StreakCalculator.Compute(distinctDates, asOf, config);
    }
}
