using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Users;

namespace Xpeak.Api.Auth.Google;

/// <summary>
/// Finds or creates an <see cref="AppUser"/> from a Google identity.
/// Matching precedence: <c>GoogleUid</c> → <c>Email</c> → create new.
/// </summary>
public sealed class GoogleAccountLinker(
    UserManager<AppUser> userManager,
    TimeProvider timeProvider)
{
    public async Task<AppUser> FindOrCreateAsync(
        GoogleIdentity identity,
        CancellationToken ct = default)
    {
        var byUid = await userManager.Users
            .FirstOrDefaultAsync(u => u.GoogleUid == identity.GoogleUid, ct);
        if (byUid is not null)
        {
            return byUid;
        }

        var byEmail = await userManager.FindByEmailAsync(identity.Email);
        if (byEmail is not null)
        {
            byEmail.GoogleUid = identity.GoogleUid;
            if (string.IsNullOrEmpty(byEmail.AvatarUrl) && !string.IsNullOrEmpty(identity.AvatarUrl))
            {
                byEmail.AvatarUrl = identity.AvatarUrl;
            }
            await userManager.UpdateAsync(byEmail);
            return byEmail;
        }

        var username = await UsernameGenerator.GenerateAsync(
            seed: identity.UsernameSeed,
            usernameExistsAsync: (candidate, cancel) =>
                userManager.Users.AnyAsync(u => u.UserName == candidate, cancel),
            cancellationToken: ct);

        var user = new AppUser
        {
            UserName = username,
            Email = identity.Email,
            EmailConfirmed = true,
            GoogleUid = identity.GoogleUid,
            AvatarUrl = identity.AvatarUrl,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                "Failed to create Google-linked user: " +
                string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}")));
        }

        return user;
    }
}
