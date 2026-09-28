using Xpeak.Api.Progression.Endpoints;
using Xpeak.Api.Progression.Repositories;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Progression;

public static class ProgressionModule
{
    public static IServiceCollection AddProgression(this IServiceCollection services)
    {
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IXpRuleRepository, XpRuleRepository>();
        services.AddScoped<IProgressionService, ProgressionService>();
        return services;
    }

    public static IEndpointRouteBuilder UseProgression(this IEndpointRouteBuilder app)
    {
        ListGroupCategoriesEndpoint.Map(app);
        return app;
    }
}
