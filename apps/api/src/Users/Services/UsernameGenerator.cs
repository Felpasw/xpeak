using System.Text.RegularExpressions;

namespace Xpeak.Api.Users;

/// <summary>
/// Turns an arbitrary string (typically an email local-part or the
/// display name from an OAuth provider) into a valid XPeak username:
///   - lowercase
///   - only [a-z0-9_]
///   - 3-20 chars
///   - not a reserved word
///   - not already taken (with `_2`, `_3`, ... suffix on collision)
///
/// Pure function: <see cref="GenerateAsync"/> asks the caller to plug in
/// a <c>usernameExistsAsync</c> delegate so this class stays free of EF
/// Core dependencies and easy to unit-test.
/// </summary>
public static partial class UsernameGenerator
{
    [GeneratedRegex(@"[^a-z0-9_]", RegexOptions.CultureInvariant)]
    private static partial Regex NonAllowed();

    /// <summary>Sanitize + pad + truncate; does not check reserved/taken.</summary>
    public static string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "user";
        }

        var lower = input.Trim().ToLowerInvariant();
        var stripped = NonAllowed().Replace(lower, string.Empty);

        if (stripped.Length < 3)
        {
            stripped = (stripped + "_user")[..Math.Min(20, (stripped + "_user").Length)];
        }
        else if (stripped.Length > 20)
        {
            stripped = stripped[..20];
        }

        return stripped;
    }

    /// <summary>
    /// Generate a unique, non-reserved username derived from
    /// <paramref name="seed"/>.
    /// </summary>
    public static async Task<string> GenerateAsync(
        string seed,
        Func<string, CancellationToken, Task<bool>> usernameExistsAsync,
        CancellationToken cancellationToken = default)
    {
        var baseName = Sanitize(seed);

        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var candidate = attempt == 0 ? baseName : Suffix(baseName, attempt + 1);

            if (ReservedUsernames.IsReserved(candidate))
            {
                continue;
            }

            if (!await usernameExistsAsync(candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException(
            $"Could not derive a unique username from seed '{seed}' within 1000 attempts.");
    }

    private static string Suffix(string baseName, int n)
    {
        var suffix = "_" + n.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var maxBase = Math.Max(3, 20 - suffix.Length);
        var truncated = baseName.Length > maxBase ? baseName[..maxBase] : baseName;
        return truncated + suffix;
    }
}
