namespace Xpeak.Api.Xp;

/// <summary>
/// Cumulative XP required to reach a given level from level 0.
/// Pure, allocation-free, no dependencies. Formula:
/// <c>XpForLevel(n) = round(100 * n^1.5)</c> for <c>n &gt;= 1</c>,
/// <c>XpForLevel(0) = 0</c>. Cheap early, painful late — reference
/// values live in <c>spec.md</c> §6.2 and are locked in tests.
/// </summary>
public static class LevelCurve
{
    public static int XpForLevel(int level)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(level);

        if (level == 0)
        {
            return 0;
        }

        return (int)Math.Round(
            100 * Math.Pow(level, 1.5),
            MidpointRounding.AwayFromZero);
    }
}
