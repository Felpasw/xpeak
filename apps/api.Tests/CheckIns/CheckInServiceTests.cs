using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CheckInServiceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_check_in_and_bumps_user_xp()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var result = await service.CreateAsync(
            user.Id,
            new CreateCheckInInput("leg day", DurationMinutes: 45, Notes: "leg day"));

        result.CheckIn.XpEarned.Should().Be(XpCalculator.CheckInXp);
        result.CheckIn.Notes.Should().Be("leg day");
        result.CheckIn.DurationMinutes.Should().Be(45);
        result.CheckIn.ScoringSnapshot.BaseXp.Should().Be(XpCalculator.CheckInXp);
        result.CheckIn.ScoringSnapshot.Total.Should().Be(XpCalculator.CheckInXp);

        result.User.Xp.Should().Be(XpCalculator.CheckInXp);
        result.User.Level.Should().Be(0);
        result.LevelUp.LeveledUp.Should().BeFalse();

        result.Streak.Current.Should().Be(1);
        result.Streak.Longest.Should().Be(1);

        var persistedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        persistedUser.Xp.Should().Be(XpCalculator.CheckInXp);
        persistedUser.Level.Should().Be(0);
    }

    [Fact]
    public async Task Crossing_the_level_boundary_reports_level_up()
    {
        var user = await SeedUserAsync();

        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Users.SingleAsync(u => u.Id == user.Id);
            tracked.Xp = 95;
            await db.SaveChangesAsync();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var result = await service.CreateAsync(user.Id, new CreateCheckInInput("test"));

        result.User.Xp.Should().Be(105);
        result.User.Level.Should().Be(1);
        result.LevelUp.LeveledUp.Should().BeTrue();
        result.LevelUp.NewLevel.Should().Be(1);
        result.LevelUp.LevelsGained.Should().Be(1);
    }

    [Fact]
    public async Task Create_with_media_persists_pending_row_without_xp_award()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var result = await service.CreateAsync(
            user.Id,
            new CreateCheckInInput("leg day", WithMedia: true));

        result.CheckIn.XpEarned.Should().Be(0);
        result.CheckIn.ScoringSnapshot.Total.Should().Be(0);

        result.User.Xp.Should().Be(0);
        result.User.Level.Should().Be(0);
        result.LevelUp.LeveledUp.Should().BeFalse();
        result.Streak.Current.Should().Be(0);
        result.Streak.Longest.Should().Be(0);

        var persisted = await db.CheckIns.AsNoTracking().SingleAsync(c => c.Id == result.CheckIn.Id);
        persisted.XpEarned.Should().Be(0);

        var persistedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        persistedUser.Xp.Should().Be(0);
        persistedUser.Level.Should().Be(0);
    }

    [Fact]
    public async Task PublishAsync_awards_xp_and_bumps_user_on_a_pending_check_in()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var created = await service.CreateAsync(
            user.Id,
            new CreateCheckInInput("leg day", WithMedia: true));

        var published = await service.PublishAsync(created.CheckIn.Id);

        published.CheckIn.XpEarned.Should().Be(XpCalculator.CheckInXp);
        published.CheckIn.ScoringSnapshot.Total.Should().Be(XpCalculator.CheckInXp);
        published.User.Xp.Should().Be(XpCalculator.CheckInXp);
        published.Streak.Current.Should().Be(1);

        var persisted = await db.CheckIns.AsNoTracking().SingleAsync(c => c.Id == created.CheckIn.Id);
        persisted.XpEarned.Should().Be(XpCalculator.CheckInXp);

        var persistedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == user.Id);
        persistedUser.Xp.Should().Be(XpCalculator.CheckInXp);
    }

    [Fact]
    public async Task PublishAsync_on_an_already_published_check_in_throws()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var created = await service.CreateAsync(user.Id, new CreateCheckInInput("leg day"));

        var act = async () => await service.PublishAsync(created.CheckIn.Id);

        await act.Should().ThrowAsync<CheckInAlreadyPublishedException>();
    }

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"svc_{suffix}", Email = $"svc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
