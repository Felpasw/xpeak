namespace Xpeak.Api.CheckIns.Services;

/// <summary>
/// Derives the current + longest streak for a <c>(user, group)</c>
/// pair from the raw <c>check_ins</c> trail. No cache table — the
/// service queries fresh every time and delegates the math to
/// <see cref="StreakCalculator"/>.
///
/// Phase 5 only supports daily-mode groups; weekly configs are
/// schema-ready but the calculator throws on them until Phase 9 / 11.
/// </summary>
public interface IStreakService
{
    Task<StreakInfo> ComputeAsync(
        Guid userId,
        Guid groupId,
        DateOnly asOf,
        CancellationToken ct = default);
}
