using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Services;

public interface IChallengeService
{
    /// <summary>
    /// Creates a challenge, auto-joins the owner as the first
    /// membership, and inserts the category whitelist — all in a
    /// single <c>SaveChangesAsync</c>. Throws
    /// <see cref="NoCategoriesException"/> on empty category list,
    /// <see cref="CategoryNotFoundException"/> when any id does not
    /// exist. On any throw, nothing is persisted.
    /// </summary>
    Task<CreateChallengeResult> CreateAsync(
        Guid ownerId,
        CreateChallengeInput input,
        CancellationToken ct = default);

    /// <summary>Lists the challenges where the user is a member,
    /// optionally filtered by state. Newest first.</summary>
    Task<IReadOnlyList<ChallengeDetail>> ListForUserAsync(
        Guid userId,
        ChallengeState? stateFilter,
        CancellationToken ct = default);

    /// <summary>Returns the challenge detail for a given caller.
    /// Visibility rule: 200 for members and for anyone on a
    /// <see cref="ChallengeVisibility.Public"/> challenge; otherwise
    /// null (endpoint maps to 404 — no leak of existence).</summary>
    Task<ChallengeDetail?> GetForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default);

    /// <summary>Hard-deletes the challenge (cascades memberships
    /// and categories). Only the owner may delete; returns true on
    /// success, false if the caller is not the owner or the row
    /// does not exist.</summary>
    Task<bool> DeleteAsync(
        Guid id,
        Guid callerId,
        CancellationToken ct = default);
}
