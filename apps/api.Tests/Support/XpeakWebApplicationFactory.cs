using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Tests.Support;

/// <summary>
/// Boots the API against a real Postgres container. One container per test class
/// (via <see cref="IAsyncLifetime"/>), reused across tests in that class.
/// </summary>
[SuppressMessage("Design", "CA1063", Justification = "IAsyncLifetime handled by xUnit.")]
public sealed class XpeakWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("xpeak_test")
        .WithUsername("xpeak")
        .WithPassword("xpeak")
        .Build();

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
            });
        });

        builder.ConfigureServices(services =>
        {
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        });
    }
}
