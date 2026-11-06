namespace Xpeak.Api.Users;

/// <summary>
/// Public projection of an <see cref="AppUser"/> safe to hand to
/// any authenticated viewer. Drops the private email; progression
/// and streak counters are kept so the public card mirrors the
/// owner's own <c>/profile</c> tabs.
/// </summary>
public sealed record PublicUserResponse(
    Guid Id,
    string Username,
    string? AvatarUrl,
    int Level,
    int Xp,
    int CurrentStreakDays,
    int LongestStreakDays,
    Backgrounds? Background,
    DateTimeOffset CreatedAt)
{
    public static PublicUserResponse From(AppUser u) => new(
        Id: u.Id,
        Username: u.UserName ?? string.Empty,
        AvatarUrl: u.AvatarUrl,
        Level: u.Level,
        Xp: u.Xp,
        CurrentStreakDays: 0,
        LongestStreakDays: 0,
        Background: u.Background,
        CreatedAt: u.CreatedAt);
}
