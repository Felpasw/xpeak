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

        // Load the user's timezone so both stored timestamps and the
        // `asOf` cursor collapse into "days" from the same frame of
        // reference. A user in Sao Paulo checking in at 22:00 local
        // and one at 08:00 next-day local must count as two distinct
        // days — using UTC would smash them into a single date.
        var user = await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == userId, ct);
        var tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);

        // Pull raw `performed_at` timestamps; convert to the user's
        // local date in memory. Bounded by the
        // (user_id, group_id, performed_at) index — cheap even at
        // years of use.
        var timestamps = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.GroupId == groupId)
            .Select(c => c.PerformedAt)
            .ToListAsync(ct);

        var distinctDates = timestamps
            .Select(t => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(t, tz).DateTime))
            .Distinct()
            .ToList();

        return StreakCalculator.Compute(distinctDates, asOf, config);
    }
}
