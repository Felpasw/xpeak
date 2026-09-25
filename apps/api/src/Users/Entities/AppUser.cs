using Microsoft.AspNetCore.Identity;

namespace Xpeak.Api.Users;

/// <summary>
/// Application user. Extends <see cref="IdentityUser{TKey}"/> (with Guid PK)
/// with XPeak-specific display and global progression fields.
///
/// Progression scope:
///  - <see cref="Xp"/> / <see cref="Level"/> are the GLOBAL player counters:
///    they aggregate every check-in the user ever made and drive the
///    all-time leaderboard, themed titles and the yearly recap. Cached
///    here for O(1) reads; kept in sync transactionally by the XP engine
///    (Phase 4+).
///  - Per-challenge progress (km, days, xp-in-challenge, etc.) lives on
///    challenge_memberships.progress -- see Phase 15.
///  - Streaks are derived on demand from check_ins, not stored here.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    public string? AvatarUrl { get; set; }

    public string? GoogleUid { get; set; }

    public int Level { get; set; }

    public int Xp { get; set; }

    /// <summary>
    /// IANA timezone identifier (e.g. <c>"America/Sao_Paulo"</c>).
    /// Defaults to <c>"UTC"</c> until proper TZ handling arrives in
    /// Phase 21 (recap). Phase 5's streak math operates in UTC only.
    /// </summary>
    public string TimeZone { get; set; } = "UTC";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
