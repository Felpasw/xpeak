using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Entities;

/// <summary>
/// A single check-in. Owned by a <see cref="Users.AppUser"/>; pays a
/// fixed XP amount (<see cref="Xp.XpCalculator.CheckInXp"/>) on
/// publish. Phase 30 removed the category and group scopes — every
/// check-in is a user-level event, and challenges observe them by
/// time window.
/// </summary>
public sealed class CheckIn
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Short label the user gave this workout ("Leg day",
    /// "Cardio manhã"). Required (max 60 chars, trimmed).</summary>
    public string Title { get; set; } = "";

    public int XpEarned { get; set; }

    public ScoringSnapshot ScoringSnapshot { get; set; } =
        new(0, [], 0);

    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;

    public int? DurationMinutes { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
