using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;

namespace Xpeak.Api.Challenges.Services;

public interface IChallengeService
{
    /// <summary>
    /// Creates a challenge and auto-joins the owner as the first
    /// membership in a single <c>SaveChangesAsync</c>.
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

    /// <summary>Hard-deletes the challenge (cascades memberships).
    /// Only the owner may delete; returns true on success, false if
    /// the caller is not the owner or the row does not exist.</summary>
    Task<bool> DeleteAsync(
        Guid id,
        Guid callerId,
        CancellationToken ct = default);

    /// <summary>Joins the caller into a challenge via a share-link
    /// <see cref="ChallengeInvitation"/>. Returns the challenge
    /// and the newly-inserted membership.</summary>
    /// <exception cref="InvitationNotFoundException">The invitation
    /// row does not exist.</exception>
    /// <exception cref="InvitationNotActiveException">The invitation
    /// is not in <see cref="ChallengeInvitationState.Pending"/>
    /// state.</exception>
    /// <exception cref="ChallengeNotActiveException">The target
    /// challenge is no longer <see cref="ChallengeState.Active"/>.
    /// </exception>
    /// <exception cref="AlreadyMemberException">The caller already
    /// belongs to the challenge.</exception>
    /// <exception cref="MemberCapReachedException">Adding the
    /// caller would exceed the challenge's member cap.</exception>
    Task<JoinChallengeResult> JoinWithInvitationAsync(
        Guid invitationId,
        Guid callerId,
        CancellationToken ct = default);
}
