using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Core;

public sealed class RevokedTokenSweeperTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public RevokedTokenSweeperTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Sweeper_deletes_only_expired_rows()
    {
        var freshJti = "sw-fresh-" + Guid.NewGuid().ToString("N");
        var staleJti = "sw-stale-" + Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        await using (var seedScope = _factory.Services.CreateAsyncScope())
        {
            var repo = seedScope.ServiceProvider.GetRequiredService<RevokedTokenRepository>();
            await repo.AddAsync(freshJti, Guid.NewGuid(), now.AddHours(1));
            await repo.AddAsync(staleJti, Guid.NewGuid(), now.AddHours(-1));
        }

        var scopeFactory = _factory.Services.GetRequiredService<IServiceScopeFactory>();
        var sweeper = new RevokedTokenSweeper(
            scopeFactory,
            TimeProvider.System,
            NullLogger<RevokedTokenSweeper>.Instance);

        using var cts = new CancellationTokenSource();
        await sweeper.StartAsync(cts.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        await sweeper.StopAsync(CancellationToken.None);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.RevokedTokens.AnyAsync(t => t.Jti == staleJti)).Should().BeFalse();
        (await db.RevokedTokens.AnyAsync(t => t.Jti == freshJti)).Should().BeTrue();
    }
}
