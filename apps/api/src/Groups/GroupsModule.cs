using Xpeak.Api.Groups.Services;

namespace Xpeak.Api.Groups;

public static class GroupsModule
{
    public static IServiceCollection AddGroups(this IServiceCollection services)
    {
        services.AddScoped<IGroupService, GroupService>();
        return services;
    }
}
