namespace Xpeak.Api.Users;

/// <summary>
/// Static block-list of usernames reserved for the platform (routes,
/// system accounts, brand names). Case-insensitive.
/// </summary>
public static class ReservedUsernames
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "administrator",
        "root",
        "system",
        "support",
        "help",
        "api",
        "auth",
        "login",
        "logout",
        "register",
        "signup",
        "signin",
        "me",
        "user",
        "users",
        "settings",
        "profile",
        "profiles",
        "xpeak",
        "official",
        "verified",
        "moderator",
        "mod",
        "bot",
        "test",
    };

    public static bool IsReserved(string? username) =>
        !string.IsNullOrWhiteSpace(username) && Reserved.Contains(username);
}
