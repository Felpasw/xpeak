using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.CheckIns.Services;

public sealed class StreakService(AppDbContext db) : IStreakService
{
    public async Task<StreakInfo> ComputeAsync(
        Guid userId,
        DateOnly asOf,
        CancellationToken ct = default)
    {
        // Load the user's timezone so stored timestamps and the `asOf`
        // cursor collapse into "days" from the same frame of reference.
        // A user in Sao Paulo checking in at 22:00 local and one at
        // 08:00 next-day local must count as two distinct days —
        // using UTC would smash them into a single date.
        var user = await db.Users
            .AsNoTracking()
            .SingleAsync(u => u.Id == userId, ct);
        var tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);

        var timestamps = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .Select(c => c.PerformedAt)
            .ToListAsync(ct);

        var distinctDates = timestamps
            .Select(t => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(t, tz).DateTime))
            .Distinct()
            .ToList();

        return StreakCalculator.Compute(distinctDates, asOf);
    }
}
