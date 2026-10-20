namespace Xpeak.Api.Challenges.Entities;

/// <summary>
/// Invitation into a challenge. The row's <see cref="Id"/> is the
/// public share token — URLs carry it verbatim and lookups go through
/// it directly. Two shapes coexist on the same table, distinguished
/// only by <see cref="InviteeUserId"/>:
/// <list type="bullet">
///   <item><b>Share-link</b>: <see cref="InviteeUserId"/> null.
///   Anyone with the invitation id can join until the owner revokes
///   the link (<see cref="State"/> ⇒ <c>Expired</c>) or the parent
///   challenge finishes.</item>
///   <item><b>Targeted</b> (Phase 16): <see cref="InviteeUserId"/>
///   populated. Only that user can accept.</item>
/// </list>
/// There is no time-based expiration or per-link usage cap: the
/// challenge's own lifecycle (<c>Active → Completed/Failed</c>) and
/// <c>MemberCap</c> are the only limits, and the join endpoint is
/// responsible for refusing joins once the challenge is no longer
/// <c>Active</c>.
/// </summary>
public sealed class ChallengeInvitation
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public Guid InvitedByUserId { get; set; }

    public Guid? InviteeUserId { get; set; }

    public ChallengeInvitationState State { get; set; } = ChallengeInvitationState.Pending;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
