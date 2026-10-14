namespace Xpeak.Api.CheckIns.Services;

public enum StreakUnit { Day, Week }

/// <summary>
/// Outcome of a streak query. <see cref="Unit"/> is always
/// <see cref="StreakUnit.Day"/> after Phase 30 collapsed the weekly
/// mode.
/// </summary>
public readonly record struct StreakInfo(int Current, int Longest, StreakUnit Unit);

/// <summary>
/// Pure static — turns a list of distinct check-in dates into a
/// <see cref="StreakInfo"/>. Zero DB, zero DI; the service layer
/// queries the dates and hands them in.
/// </summary>
public static class StreakCalculator
{
    public static StreakInfo Compute(
        IReadOnlyList<DateOnly> distinctDates,
        DateOnly asOf)
    {
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
                continue;
            }

            if (date == expected)
            {
                current++;
                expected = expected.AddDays(-1);
            }
            else
            {
                break;
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
