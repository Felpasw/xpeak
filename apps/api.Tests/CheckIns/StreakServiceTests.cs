using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class StreakServiceTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public StreakServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task ComputeAsync_returns_zero_zero_when_user_has_no_check_ins()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(user.Id, Today);

        info.Should().Be(new StreakInfo(0, 0, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_returns_one_after_a_single_check_in_today()
    {
        var user = await SeedUserAsync();
        await InsertCheckInAsync(user, Today);

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(user.Id, Today);

        info.Should().Be(new StreakInfo(1, 1, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_counts_consecutive_days_up_to_today()
    {
        var user = await SeedUserAsync();
        await InsertCheckInAsync(user, Today);
        await InsertCheckInAsync(user, Today.AddDays(-1));
        await InsertCheckInAsync(user, Today.AddDays(-2));

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(user.Id, Today);

        info.Should().Be(new StreakInfo(3, 3, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_isolates_streaks_between_users()
    {
        var alice = await SeedUserAsync();
        var bob = await SeedUserAsync();
        await InsertCheckInAsync(alice, Today);
        await InsertCheckInAsync(alice, Today.AddDays(-1));
        await InsertCheckInAsync(bob, Today);

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        (await streak.ComputeAsync(alice.Id, Today)).Current.Should().Be(2);
        (await streak.ComputeAsync(bob.Id, Today)).Current.Should().Be(1);
    }

    private static readonly TimeZoneInfo UserTz =
        TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    private static readonly DateOnly Today = DateOnly.FromDateTime(
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, UserTz));

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"strk_{suffix}", Email = $"strk_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task InsertCheckInAsync(AppUser user, DateOnly on)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "test",
            UserId = user.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = new DateTimeOffset(on.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
        });
        await db.SaveChangesAsync();
    }
}
