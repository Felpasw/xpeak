using Microsoft.AspNetCore.Identity;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Users;

/// <summary>
/// Registers the Users domain: <see cref="AppUser"/>, the Identity Core
/// stack backed by <see cref="AppDbContext"/>, roles, sign-in manager
/// and the XPeak username validator.
/// </summary>
public static class UsersModule
{
    public static IServiceCollection AddUsers(this IServiceCollection services)
    {
        services
            .AddIdentityCore<AppUser>(o =>
            {
                o.User.RequireUniqueEmail = true;
                o.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyz0123456789_";
                o.Password.RequireDigit = false;
                o.Password.RequireLowercase = false;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 8;
                o.Password.RequiredUniqueChars = 1;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddUserValidator<AppUserValidator>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
