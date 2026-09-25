using Xpeak.Api.CheckIns.Services;

namespace Xpeak.Api.CheckIns;

public static class CheckInsModule
{
    public static IServiceCollection AddCheckIns(this IServiceCollection services)
    {
        services.AddScoped<IStreakService, StreakService>();
        return services;
    }
}
