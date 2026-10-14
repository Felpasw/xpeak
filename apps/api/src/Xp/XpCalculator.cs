using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Xp;

/// <summary>
/// Turns an <see cref="XpRule"/> into the integer XP amount a single
/// check-in of that rule is worth. Pure, no dependencies. Callers load
/// the rule (via the repository) and hand it in — the calculator has
/// no opinion on where categories, groups or memberships live.
/// </summary>
public static class XpCalculator
{
    /// <summary>
    /// Hardcoded XP amount awarded per check-in. Phase 30 collapsed
    /// the per-category XP rules into this single constant; Phase 9
    /// will pick where editable config lives.
    /// </summary>
    public const int CheckInXp = 10;

    public static int Compute() => CheckInXp;

    public static int Compute(XpRule rule)
    {
        var raw = rule.BaseXp * (double)rule.WeightMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
