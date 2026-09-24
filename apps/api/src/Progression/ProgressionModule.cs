using Xpeak.Api.Progression.Repositories;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Progression;

public static class ProgressionModule
{
    public static IServiceCollection AddProgression(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProgressionService, ProgressionService>();
        return services;
    }
}
