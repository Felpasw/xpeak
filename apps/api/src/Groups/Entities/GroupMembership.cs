namespace Xpeak.Api.Groups.Entities;

/// <summary>
/// Join entity between a user and a group. Composite primary key on
/// <c>(UserId, GroupId)</c> so the same user can only sit inside a group
/// once. Cascades on both sides — deleting a user or a group drops the
/// membership rows.
/// </summary>
public sealed class GroupMembership
{
    public Guid UserId { get; set; }

    public Guid GroupId { get; set; }

    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}
