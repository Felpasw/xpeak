using Xpeak.Api.Challenges.Dto;

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
}
