namespace Xpeak.Api.Xp;

/// <summary>
/// Answers "what level does this XP amount put a user at?" and
/// "does adding this XP promote them?". Pure, static, delegates the
/// curve math to <see cref="LevelCurve"/>.
/// </summary>
public static class LevelUpService
{
    /// <summary>
    /// Highest level <c>n</c> such that <c>LevelCurve.XpForLevel(n) &lt;= xp</c>.
    /// Uses exponential-then-binary search so the hot path is
    /// O(log(level)) with no allocations.
    /// </summary>
    public static int LevelForXp(int xp)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(xp);

        if (xp == 0)
        {
            return 0;
        }

        // Exponential grow the upper bound until it overshoots.
        var hi = 1;
        while (LevelCurve.XpForLevel(hi) <= xp)
        {
            hi *= 2;
        }

        // Binary search in [hi/2, hi]; the invariant hi/2 <= answer < hi holds.
        var lo = hi / 2;
        while (lo < hi - 1)
        {
            var mid = lo + ((hi - lo) / 2);
            if (LevelCurve.XpForLevel(mid) <= xp)
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }
        return lo;
    }

    public static LevelUpResult EvaluateLevelUp(int prevXp, int newXp)
    {
        var prev = LevelForXp(prevXp);
        var next = LevelForXp(newXp);
        return new LevelUpResult(next > prev, next, next - prev);
    }
}
