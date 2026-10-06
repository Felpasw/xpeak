using Xpeak.Api.Groups;
using Xpeak.Api.Progression.Entities;

namespace Xpeak.Api.Infrastructure;

/// <summary>
/// Stable identifiers and payloads for data seeded via <c>HasData</c>.
/// Values are fixed across environments so migrations stay deterministic
/// and FKs can reference them from code and tests.
/// </summary>
internal static class SeedIds
{
    public static readonly DateTimeOffset SeedTimestamp =
        new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly Guid DefaultXpRule =
        new("00000000-0000-0000-0000-000000000010");

    public static readonly Category[] GlobalCategories =
    [
        BuildCategory("00000000-0000-0000-0000-000000000011", "pernas", "Pernas"),
        BuildCategory("00000000-0000-0000-0000-000000000012", "peito", "Peito"),
        BuildCategory("00000000-0000-0000-0000-000000000013", "costas", "Costas"),
        BuildCategory("00000000-0000-0000-0000-000000000014", "bracos", "Braços"),
        BuildCategory("00000000-0000-0000-0000-000000000015", "cardio", "Cardio"),
        BuildCategory("00000000-0000-0000-0000-000000000016", "core", "Core"),
    ];

    private static Category BuildCategory(string id, string slug, string name) => new()
    {
        Id = new Guid(id),
        GroupId = GroupIds.Global,
        XpRuleId = DefaultXpRule,
        Slug = slug,
        Name = name,
        IconPublicId = null,
        Active = true,
        CreatedAt = SeedTimestamp,
        UpdatedAt = SeedTimestamp,
    };
}
