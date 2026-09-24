namespace Xpeak.Api.Progression.Services;

/// <summary>
/// Outcome of comparing two XP totals against the level curve. Returned
/// by <see cref="LevelUpService.EvaluateLevelUp"/>; consumed by the
/// check-in transaction (Phase 5) to decide whether to emit a
/// level-up event and by which delta.
/// </summary>
public readonly record struct LevelUpResult(
    bool LeveledUp,
    int NewLevel,
    int LevelsGained);
