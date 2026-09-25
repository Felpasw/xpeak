namespace Xpeak.Api.Groups.Entities;

/// <summary>
/// 1:1 configuration row for a <see cref="Group"/>. Holds per-group
/// toggles that affect the check-in / XP flow — starting with
/// <see cref="StreakConfig"/>, extensible as later phases add more.
///
/// Seeded for the root Global group in the Phase 5 migration; every
/// sub-group created from Phase 12+ onward will get its own row via
/// the group-creation service.
/// </summary>
public sealed class GroupConfig
{
    public Guid GroupId { get; set; }

    public StreakConfig StreakConfig { get; set; } = new(StreakMode.Daily);

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
