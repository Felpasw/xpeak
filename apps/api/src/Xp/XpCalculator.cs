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
    public static int Compute(XpRule rule)
    {
        var raw = rule.BaseXp * (double)rule.WeightMultiplier;
        return (int)Math.Round(raw, MidpointRounding.AwayFromZero);
    }
}
