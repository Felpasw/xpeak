using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Services;

/// <summary>
/// Pure resolver that picks the member's current tier from an
/// ordered list and the member's challenge-scoped progress value.
/// No DI, no DbContext, no caching — the service calls this once
/// per read.
/// </summary>
public static class ChallengeTierResolver
{
    /// <summary>
    /// Picks the tier with the highest <see cref="ChallengeTier.MinLevel"/>
    /// still ≤ <paramref name="progressValue"/>. Returns null when
    /// the list is empty or the progress is below every tier.
    /// </summary>
    /// <param name="ordered">Tier list sorted by
    /// <see cref="ChallengeTier.MinLevel"/> ascending. Not
    /// validated — the service is responsible for the ordering
    /// invariant (DB index enforces uniqueness; service does the
    /// `ORDER BY min_level`).</param>
    /// <param name="progressValue">The member's current
    /// challenge-scoped progress value (0-based).</param>
    public static ChallengeTier? ResolveCurrent(
        IReadOnlyList<ChallengeTier> ordered,
        int progressValue)
    {
        ChallengeTier? current = null;
        foreach (var tier in ordered)
        {
            if (tier.MinLevel > progressValue)
            {
                break;
            }
            current = tier;
        }
        return current;
    }
}
