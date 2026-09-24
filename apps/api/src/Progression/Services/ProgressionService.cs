using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Progression.Repositories;

namespace Xpeak.Api.Progression.Services;

/// <summary>
/// Thin composition seam. Category reads go through the repository;
/// arithmetic goes through the pure static services. No business logic
/// lives here.
/// </summary>
public sealed class ProgressionService(ICategoryRepository categories) : IProgressionService
{
    public Task<IReadOnlyList<Category>> ListActiveCategoriesAsync(
        Guid groupId,
        CancellationToken ct = default) =>
        categories.ListActiveAsync(groupId, ct);

    public Task<Category?> GetCategoryBySlugAsync(
        Guid groupId,
        string slug,
        CancellationToken ct = default) =>
        categories.GetBySlugAsync(groupId, slug, ct);

    public int ComputeXp(XpRule rule) => XpCalculator.Compute(rule);

    public int XpForLevel(int level) => LevelCurve.XpForLevel(level);

    public int LevelForXp(int xp) => LevelUpService.LevelForXp(xp);

    public LevelUpResult EvaluateLevelUp(int prevXp, int newXp) =>
        LevelUpService.EvaluateLevelUp(prevXp, newXp);
}
