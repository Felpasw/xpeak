namespace Xpeak.Api.Social.Palette;

/// <summary>
/// MVP fixed palette (12 affirming emojis). Validation sits at the
/// write edge so unknown emojis never reach the repository. Opening
/// the picker to any Unicode emoji is deferred; when that lands the
/// service has to normalize skin-tone / variation-selector modifiers
/// so <c>👍</c> and <c>👍🏽</c> collapse into the same aggregate bucket.
/// </summary>
public static class ReactionPalette
{
    public static readonly IReadOnlySet<string> Emojis =
        new HashSet<string>(StringComparer.Ordinal)
        {
            "👍", "💪", "🔥", "🎯", "🎉", "👏",
            "💯", "🚀", "⚡", "🏆", "😎", "🫡",
        };

    public static bool Contains(string emoji) => Emojis.Contains(emoji);
}
