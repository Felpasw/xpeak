namespace Xpeak.Api.Media.Entities;

/// <summary>
/// A single photo or video attached to a check-in. Bytes live in the
/// configured <see cref="IMediaStorage"/> backend; this row stores
/// only the reference (<see cref="StorageKey"/>) and the metadata
/// the mobile feed needs before the asset itself is fetched
/// (dimensions for CLS-free layout, duration for the badge overlay).
/// </summary>
public sealed class CheckInMedia
{
    public Guid Id { get; set; }

    public Guid CheckInId { get; set; }

    public MediaKind Kind { get; set; }

    /// <summary>Opaque path/id inside the storage backend. Unique.</summary>
    public string StorageKey { get; set; } = "";

    public int? Width { get; set; }

    public int? Height { get; set; }

    /// <summary>Video only; null for photos.</summary>
    public int? DurationSeconds { get; set; }

    /// <summary>Position within the check-in (0-based). Ordering for
    /// carousel / grid rendering.</summary>
    public int Position { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
