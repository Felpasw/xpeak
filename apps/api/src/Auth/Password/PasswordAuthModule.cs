using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Xpeak.Api.Auth.Password;

/// <summary>
/// Registers the email/username + password authentication method:
/// failed-login counter, per-endpoint rate-limit policies, and the
/// <c>/auth/register</c> + <c>/auth/login</c> routes.
/// </summary>
public static class PasswordAuthModule
{
    public static IServiceCollection AddPasswordAuth(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services.AddSingleton<FailedLoginCounter>();

        services.Configure<RateLimiterOptions>(options =>
        {
            options.AddPolicy(RegisterEndpoint.RateLimitPolicy, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromHours(1),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));

            options.AddPolicy(LoginEndpoint.RateLimitPolicy, ctx =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(5),
                        QueueLimit = 0,
                        AutoReplenishment = true,
                    }));
        });

        return services;
    }

    public static IEndpointRouteBuilder UsePasswordAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/auth").WithTags("Auth · Password");
        RegisterEndpoint.Map(group);
        LoginEndpoint.Map(group);
        return app;
    }
}
