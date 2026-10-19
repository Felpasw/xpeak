using Xpeak.Api.Xp;

namespace Xpeak.Api.Progression.Services;

public sealed class ProgressionService : IProgressionService
{
    public int ComputeCheckInXp() => XpCalculator.Compute();

    public int XpForLevel(int level) => LevelCurve.XpForLevel(level);

    public int LevelForXp(int xp) => LevelUpService.LevelForXp(xp);

    public LevelUpResult EvaluateLevelUp(int prevXp, int newXp) =>
        LevelUpService.EvaluateLevelUp(prevXp, newXp);
}
