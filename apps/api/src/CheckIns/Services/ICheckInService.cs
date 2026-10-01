using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Services;

public interface ICheckInService
{
    /// <summary>
    /// Persists a check-in for <paramref name="userId"/> against
    /// <c>input.CategoryId</c>. When <c>input.MediaIntentCount == 0</c>,
    /// updates the user's global XP and level in the same
    /// transaction and returns the derived per-group streak.
    /// When <c>input.MediaIntentCount &gt; 0</c>, the row is
    /// persisted with <see cref="Entities.CheckInStatus.Pending"/>
    /// and <c>xp_earned = 0</c>; no XP, level or streak side-
    /// effects run. <c>PublishAsync</c> finalizes the award once
    /// media uploads complete.
    ///
    /// Throws <see cref="CategoryNotFoundException"/> when the
    /// category id is unknown, <see cref="NotAGroupMemberException"/>
    /// when the caller does not belong to the category's group.
    /// </summary>
    Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default);

    /// <summary>
    /// Finalizes a pending check-in: computes XP from the category
    /// rule, flips <c>status</c> to <see cref="Entities.CheckInStatus.Published"/>,
    /// bumps the owning user's XP/level in one transaction, and
    /// returns the derived per-group streak. Idempotent-by-throw:
    /// a second call on an already-published row raises
    /// <see cref="CheckInAlreadyPublishedException"/>.
    /// </summary>
    Task<CreateCheckInResult> PublishAsync(
        Guid checkInId,
        CancellationToken ct = default);
}
