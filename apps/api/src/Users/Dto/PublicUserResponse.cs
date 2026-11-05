namespace Xpeak.Api.Users;

/// <summary>
/// Public projection of an <see cref="AppUser"/> safe to hand to
/// any authenticated viewer. Drops private fields (email, streak
/// counters) that are exposed only on <c>/me</c>.
/// </summary>
public sealed record PublicUserResponse(
    Guid Id,
    string Username,
    string? AvatarUrl,
    int Level,
    int Xp,
    Backgrounds? Background,
    DateTimeOffset CreatedAt)
{
    public static PublicUserResponse From(AppUser u) => new(
        Id: u.Id,
        Username: u.UserName ?? string.Empty,
        AvatarUrl: u.AvatarUrl,
        Level: u.Level,
        Xp: u.Xp,
        Background: u.Background,
        CreatedAt: u.CreatedAt);
}
