using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

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
    public async Task Happy_path_persists_check_in_and_bumps_user_xp_and_level()
    {
        var fixture = await SeedFixtureAsync(baseXp: 15, weight: 1.40m);
        await JoinGroupAsync(fixture);

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var result = await service.CreateAsync(
            fixture.User.Id,
            new CreateCheckInInput(fixture.Category.Id, DurationMinutes: 45, Notes: "leg day"));

        result.CheckIn.XpEarned.Should().Be(21);
        result.CheckIn.GroupId.Should().Be(fixture.Group.Id);
        result.CheckIn.CategoryId.Should().Be(fixture.Category.Id);
        result.CheckIn.Notes.Should().Be("leg day");
        result.CheckIn.DurationMinutes.Should().Be(45);
        result.CheckIn.ScoringSnapshot.BaseXp.Should().Be(15);
        result.CheckIn.ScoringSnapshot.Total.Should().Be(21);

        result.User.Xp.Should().Be(21);
        result.User.Level.Should().Be(0); // 21 XP not enough for level 1 (100)
        result.LevelUp.LeveledUp.Should().BeFalse();

        result.Streak.Current.Should().Be(1);
        result.Streak.Longest.Should().Be(1);

        var persistedUser = await db.Users.AsNoTracking().SingleAsync(u => u.Id == fixture.User.Id);
        persistedUser.Xp.Should().Be(21);
        persistedUser.Level.Should().Be(0);
    }

    [Fact]
    public async Task Crossing_the_level_boundary_reports_level_up()
    {
        var fixture = await SeedFixtureAsync(baseXp: 100, weight: 1.00m);
        await JoinGroupAsync(fixture);

        // Pre-load user with 90 XP so this check-in (100 XP) crosses level 1 (needs 100).
        await using (var setupScope = _factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.SingleAsync(u => u.Id == fixture.User.Id);
            user.Xp = 90;
            await db.SaveChangesAsync();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var result = await service.CreateAsync(
            fixture.User.Id,
            new CreateCheckInInput(fixture.Category.Id));

        result.User.Xp.Should().Be(190);
        result.User.Level.Should().Be(1);
        result.LevelUp.LeveledUp.Should().BeTrue();
        result.LevelUp.NewLevel.Should().Be(1);
        result.LevelUp.LevelsGained.Should().Be(1);
    }

    [Fact]
    public async Task Rejects_when_user_is_not_a_member_of_the_categorys_group()
    {
        // Fixture creates group + category but does NOT join the user.
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var act = async () => await service.CreateAsync(
            fixture.User.Id,
            new CreateCheckInInput(fixture.Category.Id));

        await act.Should().ThrowAsync<NotAGroupMemberException>();

        // No row inserted.
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.CheckIns.CountAsync(c => c.UserId == fixture.User.Id);
        count.Should().Be(0);
    }

    [Fact]
    public async Task Rejects_when_category_id_is_unknown()
    {
        var fixture = await SeedFixtureAsync();
        await JoinGroupAsync(fixture);

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var act = async () => await service.CreateAsync(
            fixture.User.Id,
            new CreateCheckInInput(Guid.NewGuid()));

        await act.Should().ThrowAsync<CategoryNotFoundException>();

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.CheckIns.CountAsync(c => c.UserId == fixture.User.Id);
        count.Should().Be(0);
    }

    [Fact]
    public async Task Populates_group_id_from_the_category_regardless_of_context()
    {
        // The client can't override group_id — it's server-populated from
        // the loaded category. Even a service consumer can't inject a
        // wrong value because the input DTO doesn't have a GroupId field.
        var fixture = await SeedFixtureAsync();
        await JoinGroupAsync(fixture);

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<ICheckInService>();

        var result = await service.CreateAsync(
            fixture.User.Id,
            new CreateCheckInInput(fixture.Category.Id));

        result.CheckIn.GroupId.Should().Be(fixture.Category.GroupId);
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private async Task<SeededFixture> SeedFixtureAsync(int baseXp = 10, decimal weight = 1.0m)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"svc_{suffix}", Email = $"svc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = baseXp, WeightMultiplier = weight };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"s_{suffix}",
            Name = "Test",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new SeededFixture(user, group, category);
    }

    private async Task JoinGroupAsync(SeededFixture fixture)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.GroupMemberships.Add(new GroupMembership
        {
            UserId = fixture.User.Id,
            GroupId = fixture.Group.Id,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
