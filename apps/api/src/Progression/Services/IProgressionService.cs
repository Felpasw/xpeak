using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Progression.Services;

/// <summary>
/// Only orchestration surface the Progression module exports. Composes
/// <see cref="ICategoryRepository"/> reads with the pure static
/// arithmetic (<see cref="XpCalculator"/>, <see cref="LevelCurve"/>,
/// <see cref="LevelUpService"/>). Downstream modules (check-in in
/// Phase 5, rankings later, LLM tools) depend on this interface, not
/// on the repository or the pure services directly.
/// </summary>
public interface IProgressionService
{
    Task<IReadOnlyList<Category>> ListActiveCategoriesAsync(
        Guid groupId,
        CancellationToken ct = default);

    Task<Category?> GetCategoryBySlugAsync(
        Guid groupId,
        string slug,
        CancellationToken ct = default);

    Task<Category?> GetCategoryByIdAsync(
        Guid categoryId,
        CancellationToken ct = default);

    Task<XpRule?> GetRuleAsync(
        Guid ruleId,
        CancellationToken ct = default);

    int ComputeXp(XpRule rule);

    int XpForLevel(int level);

    int LevelForXp(int xp);

    LevelUpResult EvaluateLevelUp(int prevXp, int newXp);
}
