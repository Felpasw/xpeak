using FluentValidation;
using Xpeak.Api.CheckIns.Endpoints;
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
        services.AddValidatorsFromAssemblyContaining(typeof(CheckInsModule));
        return services;
    }

    public static IEndpointRouteBuilder UseCheckIns(this IEndpointRouteBuilder app)
    {
        CreateCheckInEndpoint.Map(app);
        ListCheckInsEndpoint.Map(app);
        return app;
    }
}
