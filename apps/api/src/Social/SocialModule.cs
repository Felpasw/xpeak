using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social;

/// <summary>
/// Registers the visibility seam. Reactions, comments and the
/// endpoint registration (<c>UseSocial</c>) are added as later
/// commits ship.
/// </summary>
public static class SocialModule
{
    public static IServiceCollection AddSocial(this IServiceCollection services)
    {
        services.AddScoped<ICheckInVisibility, OpenCheckInVisibility>();
        return services;
    }
}
