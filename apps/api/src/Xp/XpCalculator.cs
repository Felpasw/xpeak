namespace Xpeak.Api.Xp;

/// <summary>
/// Turns the hardcoded <see cref="CheckInXp"/> payout into an integer.
/// Phase 30 collapsed the per-category XP rules into this single
/// constant; Phase 9 will pick where editable config lives.
/// </summary>
public static class XpCalculator
{
    public const int CheckInXp = 10;

    public static int Compute() => CheckInXp;
}
