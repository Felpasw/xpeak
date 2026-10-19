using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Services;

public interface ICheckInService
{
    /// <summary>
    /// Persists a check-in for <paramref name="userId"/>. When
    /// <c>input.WithMedia</c> is false, updates the user's XP + level
    /// in the same transaction and returns the derived per-user
    /// streak. When <c>input.WithMedia</c> is true, the row is
    /// persisted with <c>xp_earned = 0</c>; no XP, level or streak
    /// side-effects run. <c>PublishAsync</c> finalizes the award
    /// once media uploads complete.
    /// </summary>
    Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default);

    /// <summary>
    /// Finalizes a pending check-in: pays
    /// <see cref="Xp.XpCalculator.CheckInXp"/>, bumps the owning user's
    /// XP/level in one transaction, and returns the derived per-user
    /// streak. Idempotent-by-throw: a second call on an already-
    /// published row raises <see cref="CheckInAlreadyPublishedException"/>.
    /// </summary>
    Task<CreateCheckInResult> PublishAsync(
        Guid checkInId,
        CancellationToken ct = default);
}
