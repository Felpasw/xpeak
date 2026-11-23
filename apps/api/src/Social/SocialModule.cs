using FluentValidation;
using Xpeak.Api.Social.Endpoints;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social;

/// <summary>
/// Registers reactions and the visibility seam. Comments ship in a
/// later commit.
/// </summary>
public static class SocialModule
{
    public static IServiceCollection AddSocial(this IServiceCollection services)
    {
        services.AddScoped<ICheckInVisibility, OpenCheckInVisibility>();
        services.AddScoped<ICheckInReactionRepository, CheckInReactionRepository>();
        services.AddValidatorsFromAssemblyContaining(typeof(SocialModule));
        return services;
    }

    public static IEndpointRouteBuilder UseSocial(this IEndpointRouteBuilder app)
    {
        ToggleReactionEndpoint.Map(app);
        GetReactionsEndpoint.Map(app);
        return app;
    }
}
