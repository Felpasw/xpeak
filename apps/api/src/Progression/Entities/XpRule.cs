using System.ComponentModel.DataAnnotations;

namespace Xpeak.Api.Progression.Entities;

/// <summary>
/// Scoring rule that turns a check-in into an integer XP amount.
///
/// Extracted from <see cref="Category"/> on purpose: the same shape
/// (<c>BaseXp</c> + <c>WeightMultiplier</c>) will also apply to
/// group-level bonuses (Phase 7), challenge modifiers (Phase 15+) and
/// user overrides (Phase 9). Keeping the rule as its own entity avoids
/// replicating the fields across every consumer and lets Phase 9 version
/// / audit rules independently of what references them.
/// </summary>
public sealed class XpRule
{
    public Guid Id { get; set; }

    [Range(1, int.MaxValue)]
    public int BaseXp { get; set; }

    [Range(typeof(decimal), "0.5", "2.5")]
    public decimal WeightMultiplier { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
