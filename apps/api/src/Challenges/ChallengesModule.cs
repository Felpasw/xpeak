using Xpeak.Api.Challenges.Services;

namespace Xpeak.Api.Challenges;

public static class ChallengesModule
{
    public static IServiceCollection AddChallenges(this IServiceCollection services)
    {
        services.AddScoped<IChallengeService, ChallengeService>();
        return services;
    }
}
