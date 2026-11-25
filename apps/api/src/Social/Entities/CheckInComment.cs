namespace Xpeak.Api.Social.Entities;

/// <summary>
/// A flat text comment a user left on a check-in. Body capped at
/// 280 chars (same cap as check-in notes in Phase 5). Soft-deleted
/// via <see cref="DeletedAt"/> so the audit trail survives; live
/// reads filter the deleted rows out at the query layer.
/// </summary>
public sealed class CheckInComment
{
    public Guid Id { get; set; }

    public Guid CheckInId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Required, trimmed, 1..280 chars.</summary>
    public string Body { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Soft-delete marker. When non-null the row is
    /// hidden from the live feed; kept for audit / future
    /// moderation review.</summary>
    public DateTimeOffset? DeletedAt { get; set; }
}
