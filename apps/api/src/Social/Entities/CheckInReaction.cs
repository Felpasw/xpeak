namespace Xpeak.Api.Social.Entities;

/// <summary>
/// One emoji reaction a user left on a check-in. Carries no
/// narrative weight on its own — toggling an existing
/// <c>(check_in_id, user_id, emoji)</c> triple hard-deletes the
/// row (no soft-delete). Unique on the triple so the same user
/// cannot double-stack the same emoji; distinct emojis from the
/// same user coexist.
/// </summary>
public sealed class CheckInReaction
{
    public Guid Id { get; set; }

    public Guid CheckInId { get; set; }

    public Guid UserId { get; set; }

    /// <summary>One of the MVP palette emojis
    /// (<see cref="Xpeak.Api.Social.Palette.ReactionPalette"/>).
    /// Validation keeps anything else out at the write edge.
    /// </summary>
    public string Emoji { get; set; } = "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
