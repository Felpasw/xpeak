using Xpeak.Api.CheckIns.Repositories;
using Xpeak.Api.CheckIns.Services;

namespace Xpeak.Api.CheckIns;

public static class CheckInsModule
{
    public static IServiceCollection AddCheckIns(this IServiceCollection services)
    {
        services.AddScoped<ICheckInRepository, CheckInRepository>();
        services.AddScoped<IStreakService, StreakService>();
        services.AddScoped<ICheckInService, CheckInService>();
        return services;
    }
}
