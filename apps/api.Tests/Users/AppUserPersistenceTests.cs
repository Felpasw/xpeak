using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Users;

public sealed class AppUserPersistenceTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public AppUserPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Rejects_duplicate_username_case_insensitively()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var first = await users.CreateAsync(
            new AppUser { UserName = $"dup_{suffix}", Email = $"a_{suffix}@x.com" },
            "hunter22!");
        first.Succeeded.Should().BeTrue();

        var duplicate = await users.CreateAsync(
            new AppUser { UserName = $"DUP_{suffix}".ToLowerInvariant(), Email = $"b_{suffix}@x.com" },
            "hunter22!");

        duplicate.Succeeded.Should().BeFalse();
        duplicate.Errors.Should().Contain(e => e.Code.StartsWith("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_duplicate_email_case_insensitively()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var first = await users.CreateAsync(
            new AppUser { UserName = $"mail_a_{suffix}", Email = $"same_{suffix}@x.com" },
            "hunter22!");
        first.Succeeded.Should().BeTrue();

        var duplicate = await users.CreateAsync(
            new AppUser { UserName = $"mail_b_{suffix}", Email = $"SAME_{suffix}@X.COM" },
            "hunter22!");

        duplicate.Succeeded.Should().BeFalse();
        duplicate.Errors.Should().Contain(e => e.Code.StartsWith("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Persists_progression_defaults_as_zero()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"prog_{suffix}", Email = $"prog_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var reloaded = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        reloaded.Level.Should().Be(0);
        reloaded.Xp.Should().Be(0);
    }
}
