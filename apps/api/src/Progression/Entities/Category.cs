namespace Xpeak.Api.Progression.Entities;

/// <summary>
/// Workout category. Owned by a <c>Group</c>: the global group holds the
/// system defaults, sub-groups can register their own copies with their
/// own scoring rules. The XP values live on <see cref="XpRule"/> — a
/// category is the identity + visual + scope, not the numbers.
/// </summary>
public sealed class Category
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid XpRuleId { get; set; }

    public string Slug { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? IconPublicId { get; set; }

    public bool Active { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
