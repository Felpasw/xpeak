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
using Microsoft.Extensions.Hosting;
using Npgsql;
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
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Run migrations through the host's own NpgsqlDataSource
        // singleton so the OID cache we populate below is the one
        // tests will reuse. Doing this via a sidecar provider would
        // create a different singleton and leave the real one with a
        // stale cache (unaware of the enum types the migration just
        // created, so Npgsql would fall back to int32 bindings).
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        // Migrations create native Postgres enums after the data
        // source has already opened its first connection, so its
        // type OID cache is stale. Reload once before tests run so
        // enum bindings resolve instead of falling back to integer.
        var dataSource = host.Services.GetRequiredService<NpgsqlDataSource>();
        using var reloadConn = dataSource.OpenConnection();
        reloadConn.ReloadTypes();

        return host;
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
