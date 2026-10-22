using FluentValidation;
using Xpeak.Api.Challenges.Endpoints;
using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges;

public static class ChallengesModule
{
    public static IServiceCollection AddChallenges(this IServiceCollection services)
    {
        services.AddScoped<IChallengeService, ChallengeService>();
        services.AddScoped<IProgressCalculator, ProgressCalculator>();
        services.AddValidatorsFromAssemblyContaining(typeof(ChallengesModule));
        return services;
    }

    public static IEndpointRouteBuilder UseChallenges(this IEndpointRouteBuilder app)
    {
        CreateChallengeEndpoint.Map(app);
        ListChallengesEndpoint.Map(app);
        GetChallengeEndpoint.Map(app);
        DeleteChallengeEndpoint.Map(app);
        GetChallengeProgressEndpoint.Map(app);
        JoinChallengeEndpoint.Map(app);
        EndChallengeEndpoint.Map(app);
        PresignTierImageEndpoint.Map(app);
        return app;
    }
}
