using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Entities;

/// <summary>
/// A single check-in. Owned by a <see cref="Users.AppUser"/>, scoped
/// to a category (which lives inside a group). <c>GroupId</c> is
/// denormalized from the category at insert time so per-group
/// queries (ranking, timeline) stay single-table and the historical
/// scope is immutable if a category is ever moved between groups.
/// </summary>
public sealed class CheckIn
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid CategoryId { get; set; }

    public Guid GroupId { get; set; }

    public int XpEarned { get; set; }

    public ScoringSnapshot ScoringSnapshot { get; set; } =
        new(0, [], 0);

    public DateTimeOffset PerformedAt { get; set; } = DateTimeOffset.UtcNow;

    public int? DurationMinutes { get; set; }

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
