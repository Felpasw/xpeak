namespace Xpeak.Api.Groups.Entities;

/// <summary>
/// A group aggregates users, categories and their own XP rules. Every user
/// belongs to the root group (<see cref="GroupIds.Global"/>) plus zero or
/// more sub-groups. Sub-groups arrive in Phase 12+; for now the schema
/// exists so the domain model doesn't have to be re-shaped later.
/// </summary>
public sealed class Group
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// True only for the single global group seeded by the migration.
    /// Sub-groups always land with <c>IsRoot = false</c>.
    /// </summary>
    public bool IsRoot { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
