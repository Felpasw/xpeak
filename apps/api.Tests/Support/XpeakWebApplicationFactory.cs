using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xpeak.Api.Auth.Password;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media;
using Xpeak.Api.Media.Providers.Fake;

namespace Xpeak.Api.Tests.Support;

/// <summary>
/// Boots the API against a real Postgres container. One container per test class
/// (via <see cref="IAsyncLifetime"/>), reused across tests in that class.
/// Rate limits are disabled by default so tests can hammer endpoints without
/// tripping 429; the dedicated rate-limit tests use
/// <see cref="RateLimitedXpeakWebApplicationFactory"/> instead.
/// </summary>
[SuppressMessage("Design", "CA1063", Justification = "IAsyncLifetime handled by xUnit.")]
public class XpeakWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("xpeak_test")
        .WithUsername("xpeak")
        .WithPassword("xpeak")
        .Build();

    protected virtual bool DisableRateLimits => true;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:Key"] = "test-jwt-key-do-not-use-in-prod-must-be-32-bytes+++",
                ["Jwt:Issuer"] = "xpeak",
                ["Jwt:Audience"] = "xpeak",
                ["Google:ClientId"] = "test-client-id",
                ["Google:ClientSecret"] = "test-client-secret",
                ["Auth:CallbackUri"] = "xpeak://auth/callback",
                ["Media:Provider"] = "fake",
            });
        });

        builder.ConfigureServices(services =>
        {
            if (DisableRateLimits)
            {
                services.PostConfigure<RateLimiterOptions>(ReplaceWithNoLimiter);
            }

            // MediaModule.AddMedia is eager on configuration, so the
            // in-memory `Media:Provider = fake` override above lands
            // too late to influence the initial provider pick. Force
            // the fake singleton here so tests can seed metadata.
            services.RemoveAll<IMediaStorage>();
            services.AddSingleton<IMediaStorage, FakeMediaStorage>();

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }

    private static void ReplaceWithNoLimiter(RateLimiterOptions options)
    {
        var mapProperty = typeof(RateLimiterOptions)
            .GetProperty("PolicyMap", BindingFlags.NonPublic | BindingFlags.Instance);
        if (mapProperty?.GetValue(options) is IDictionary dict)
        {
            dict.Clear();
        }

        options.AddPolicy(RegisterEndpoint.RateLimitPolicy, _ =>
            RateLimitPartition.GetNoLimiter<string>("test"));
        options.AddPolicy(LoginEndpoint.RateLimitPolicy, _ =>
            RateLimitPartition.GetNoLimiter<string>("test"));
    }
}

/// <summary>Same as <see cref="XpeakWebApplicationFactory"/> but keeps the
/// production rate-limit policies intact so the dedicated tests can trip them.</summary>
public sealed class RateLimitedXpeakWebApplicationFactory : XpeakWebApplicationFactory
{
    protected override bool DisableRateLimits => false;
}
