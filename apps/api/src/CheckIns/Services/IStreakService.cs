namespace Xpeak.Api.CheckIns.Services;

/// <summary>
/// Derives the current + longest streak for a user from the raw
/// <c>check_ins</c> trail. No cache table — the service queries fresh
/// every time and delegates the math to <see cref="StreakCalculator"/>.
///
/// Phase 30 collapsed the per-group streak into a per-user streak.
/// Daily mode is the only mode supported.
/// </summary>
public interface IStreakService
{
    Task<StreakInfo> ComputeAsync(
        Guid userId,
        DateOnly asOf,
        CancellationToken ct = default);
}
