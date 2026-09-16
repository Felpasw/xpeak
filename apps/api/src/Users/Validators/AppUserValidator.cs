using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace Xpeak.Api.Users;

/// <summary>
/// Layered on top of Identity's default validator. Enforces the XPeak
/// username rules (lowercase alphanumeric + underscore, 3-20 chars,
/// reserved words blocked). Email validation and uniqueness stay on
/// Identity's built-in machinery.
/// </summary>
public sealed partial class AppUserValidator : IUserValidator<AppUser>
{
    [GeneratedRegex(@"^[a-z0-9_]{3,20}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernameRegex();

    public Task<IdentityResult> ValidateAsync(UserManager<AppUser> manager, AppUser user)
    {
        var errors = new List<IdentityError>();
        var username = user.UserName;

        if (string.IsNullOrWhiteSpace(username))
        {
            errors.Add(new IdentityError
            {
                Code = "UsernameRequired",
                Description = "Username is required.",
            });
        }
        else
        {
            if (!UsernameRegex().IsMatch(username))
            {
                errors.Add(new IdentityError
                {
                    Code = "UsernameFormat",
                    Description = "Username must be 3-20 lowercase letters, digits or underscores.",
                });
            }

            if (ReservedUsernames.IsReserved(username))
            {
                errors.Add(new IdentityError
                {
                    Code = "UsernameReserved",
                    Description = "Username is reserved.",
                });
            }
        }

        return Task.FromResult(errors.Count == 0
            ? IdentityResult.Success
            : IdentityResult.Failed([.. errors]));
    }
}
