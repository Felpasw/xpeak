namespace Xpeak.Api.Groups;

/// <summary>
/// Well-known group identifiers. Stable across every environment so
/// migrations, tests and downstream FKs can reference them by value.
/// </summary>
public static class GroupIds
{
    /// <summary>
    /// The root group every user joins on registration. Holds the global
    /// XP totals and drives the all-time ranking. Cannot be deleted.
    /// </summary>
    public static readonly Guid Global =
        new("00000000-0000-0000-0000-000000000001");
}
