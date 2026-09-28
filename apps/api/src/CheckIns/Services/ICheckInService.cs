using Xpeak.Api.CheckIns.Dto;

namespace Xpeak.Api.CheckIns.Services;

public interface ICheckInService
{
    /// <summary>
    /// Persists a check-in for <paramref name="userId"/> against
    /// <c>input.CategoryId</c>, updates the user's global XP and
    /// level in the same transaction, and returns the derived
    /// per-group streak.
    ///
    /// Throws <see cref="CategoryNotFoundException"/> when the
    /// category id is unknown, <see cref="NotAGroupMemberException"/>
    /// when the caller does not belong to the category's group.
    /// </summary>
    Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default);
}
