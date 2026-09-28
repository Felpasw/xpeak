using Xpeak.Api.Groups.Entities;

namespace Xpeak.Api.CheckIns.Services;

public enum StreakUnit { Day, Week }

/// <summary>
/// Outcome of a streak query. <see cref="Unit"/> reflects the
/// group's <see cref="StreakConfig.Mode"/> — Phase 5 always returns
/// <see cref="StreakUnit.Day"/> because only daily mode is
/// implemented.
/// </summary>
public readonly record struct StreakInfo(int Current, int Longest, StreakUnit Unit);

/// <summary>
/// Pure static — turns a list of distinct check-in dates plus a
/// group's <see cref="StreakConfig"/> into a <see cref="StreakInfo"/>.
/// Zero DB, zero DI; the service layer (StreakService) queries the
/// dates and hands them in.
///
/// Phase 5 only implements <see cref="StreakMode.Daily"/>; other
/// modes throw <see cref="NotSupportedException"/> so callers fail
/// loud instead of silently defaulting.
/// </summary>
public static class StreakCalculator
{
    public static StreakInfo Compute(
        IReadOnlyList<DateOnly> distinctDates,
        DateOnly asOf,
        StreakConfig config)
    {
        if (config.Mode != StreakMode.Daily)
        {
            throw new NotSupportedException(
                $"Streak mode '{config.Mode}' is not implemented in Phase 5.");
        }

        // Sort descending in place-safe way (input may be in any order).
        var descending = distinctDates
            .Distinct()
            .OrderByDescending(d => d)
            .ToArray();

        var current = ComputeCurrent(descending, asOf);
        var longest = ComputeLongest(descending);

        return new StreakInfo(current, longest, StreakUnit.Day);
    }

    private static int ComputeCurrent(DateOnly[] descending, DateOnly asOf)
    {
        var current = 0;
        var expected = asOf;
        foreach (var date in descending)
        {
            if (date > asOf)
            {
                continue;   // future dates: ignore
            }

            if (date == expected)
            {
                current++;
                expected = expected.AddDays(-1);
            }
            else
            {
                break;       // gap — streak is broken
            }
        }
        return current;
    }

    private static int ComputeLongest(DateOnly[] descending)
    {
        if (descending.Length == 0)
        {
            return 0;
        }

        var longest = 1;
        var run = 1;
        for (var i = 1; i < descending.Length; i++)
        {
            if (descending[i] == descending[i - 1].AddDays(-1))
            {
                run++;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 1;
            }
        }
        return longest;
    }
}
