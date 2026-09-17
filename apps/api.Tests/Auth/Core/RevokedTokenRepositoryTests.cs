using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Auth.Core;

public sealed class RevokedTokenRepositoryTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public RevokedTokenRepositoryTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Add_then_Exists_round_trips()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<RevokedTokenRepository>();
        var jti = Guid.NewGuid().ToString("N");

        (await repo.ExistsAsync(jti)).Should().BeFalse();

        await repo.AddAsync(jti, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));

        (await repo.ExistsAsync(jti)).Should().BeTrue();
    }

    [Fact]
    public async Task Add_is_idempotent_for_same_jti()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<RevokedTokenRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var jti = Guid.NewGuid().ToString("N");

        await repo.AddAsync(jti, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        await repo.AddAsync(jti, Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(2));

        var count = await db.RevokedTokens.CountAsync(t => t.Jti == jti);
        count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteExpired_only_removes_rows_past_expiry()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<RevokedTokenRepository>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var freshJti = "fresh-" + Guid.NewGuid().ToString("N");
        var staleJti = "stale-" + Guid.NewGuid().ToString("N");
        var now = DateTimeOffset.UtcNow;

        await repo.AddAsync(freshJti, Guid.NewGuid(), now.AddHours(1));
        await repo.AddAsync(staleJti, Guid.NewGuid(), now.AddHours(-1));

        var deleted = await repo.DeleteExpiredAsync(now);

        deleted.Should().BeGreaterThanOrEqualTo(1);
        (await db.RevokedTokens.AnyAsync(t => t.Jti == staleJti)).Should().BeFalse();
        (await db.RevokedTokens.AnyAsync(t => t.Jti == freshJti)).Should().BeTrue();
    }
}
