using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
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
    public async Task ComputeAsync_returns_zero_zero_when_user_has_no_check_ins_in_group()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(fixture.User.Id, fixture.Group.Id, Today);

        info.Should().Be(new StreakInfo(0, 0, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_returns_one_after_a_single_check_in_today()
    {
        var fixture = await SeedFixtureAsync();
        await InsertCheckInAsync(fixture, Today);

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(fixture.User.Id, fixture.Group.Id, Today);

        info.Should().Be(new StreakInfo(1, 1, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_counts_consecutive_days_up_to_today()
    {
        var fixture = await SeedFixtureAsync();
        await InsertCheckInAsync(fixture, Today);
        await InsertCheckInAsync(fixture, Today.AddDays(-1));
        await InsertCheckInAsync(fixture, Today.AddDays(-2));

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var info = await streak.ComputeAsync(fixture.User.Id, fixture.Group.Id, Today);

        info.Should().Be(new StreakInfo(3, 3, StreakUnit.Day));
    }

    [Fact]
    public async Task ComputeAsync_isolates_streaks_between_two_groups_of_the_same_user()
    {
        var fixture = await SeedFixtureAsync();
        await InsertCheckInAsync(fixture, Today);
        await InsertCheckInAsync(fixture, Today.AddDays(-1));

        // Second group for the same user, only one check-in today.
        var second = await SeedSecondGroupAsync(fixture);
        await InsertCheckInAsync(fixture with { Group = second.Group, Category = second.Category }, Today);

        await using var scope = _factory.Services.CreateAsyncScope();
        var streak = scope.ServiceProvider.GetRequiredService<IStreakService>();

        var first = await streak.ComputeAsync(fixture.User.Id, fixture.Group.Id, Today);
        var other = await streak.ComputeAsync(fixture.User.Id, second.Group.Id, Today);

        first.Current.Should().Be(2);
        other.Current.Should().Be(1);
    }

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"strk_{suffix}", Email = $"strk_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"s_{suffix}",
            Name = "T",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new SeededFixture(user, group, category);
    }

    private async Task<SeededFixture> SeedSecondGroupAsync(SeededFixture existing)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group { Id = Guid.NewGuid(), Name = $"g2_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"s2_{suffix}",
            Name = "T2",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new SeededFixture(existing.User, group, category);
    }

    private async Task InsertCheckInAsync(SeededFixture fixture, DateOnly on)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = fixture.User.Id,
            CategoryId = fixture.Category.Id,
            GroupId = fixture.Group.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(
                10,
                [new ScoringMultiplier("category_weight", 1.00m)],
                10),
            PerformedAt = new DateTimeOffset(on.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero),
        });
        await db.SaveChangesAsync();
    }
}
