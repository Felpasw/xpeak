using Xpeak.Api.Xp;

namespace Xpeak.Api.Progression.Services;

/// <summary>
/// Pure arithmetic surface the Progression module exposes to the rest
/// of the app. Composition seam over the static services in
/// <see cref="XpCalculator"/>, <see cref="LevelCurve"/> and
/// <see cref="LevelUpService"/>. No persistence surface — Phase 30
/// collapsed the per-category XP rules into the single constant
/// <see cref="XpCalculator.CheckInXp"/>.
/// </summary>
public interface IProgressionService
{
    int ComputeCheckInXp();

    int XpForLevel(int level);

    int LevelForXp(int xp);

    LevelUpResult EvaluateLevelUp(int prevXp, int newXp);
}
