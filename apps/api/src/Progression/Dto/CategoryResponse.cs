using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Progression.Dto;

/// <summary>
/// Wire shape for a category — the mobile check-in screen consumes
/// this to render the picker grid. XP scoring fields
/// (<c>base_xp</c>, <c>weight_multiplier</c>) are intentionally
/// omitted; the server owns those and returns them inside the
/// check-in's <c>scoring_snapshot</c> after the fact.
/// </summary>
public sealed record CategoryResponse(
    Guid Id,
    Guid GroupId,
    string Slug,
    string Name,
    string? IconPublicId,
    bool Active)
{
    public static CategoryResponse From(Category c) => new(
        c.Id,
        c.GroupId,
        c.Slug,
        c.Name,
        c.IconPublicId,
        c.Active);
}

public sealed record ListCategoriesResponse(IReadOnlyList<CategoryResponse> Categories);
