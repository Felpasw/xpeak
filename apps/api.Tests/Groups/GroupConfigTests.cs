using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;

namespace Xpeak.Api.Tests.Groups;

public sealed class GroupConfigTests : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GroupConfigTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Global_group_has_a_daily_config_after_migration()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var config = await db.GroupConfigs
            .AsNoTracking()
            .SingleAsync(gc => gc.GroupId == GroupIds.Global);

        config.StreakConfig.Mode.Should().Be(StreakMode.Daily);
        config.StreakConfig.RequiredDaysPerWeek.Should().BeNull();
        config.StreakConfig.WeekStart.Should().BeNull();
    }

    [Fact]
    public async Task Streak_config_json_round_trips_through_ef_conversion()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group { Id = Guid.NewGuid(), Name = $"cfg_{suffix}", IsRoot = false };
        var config = new GroupConfig
        {
            GroupId = group.Id,
            StreakConfig = new StreakConfig(
                StreakMode.Weekly,
                RequiredDaysPerWeek: 3,
                WeekStart: DayOfWeek.Monday),
        };
        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        await db.SaveChangesAsync();

        await using var scope2 = _factory.Services.CreateAsyncScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await db2.GroupConfigs
            .AsNoTracking()
            .SingleAsync(gc => gc.GroupId == group.Id);

        reloaded.StreakConfig.Mode.Should().Be(StreakMode.Weekly);
        reloaded.StreakConfig.RequiredDaysPerWeek.Should().Be(3);
        reloaded.StreakConfig.WeekStart.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task Deleting_a_group_cascades_to_its_config()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group { Id = Guid.NewGuid(), Name = $"cas_{suffix}", IsRoot = false };
        var config = new GroupConfig
        {
            GroupId = group.Id,
            StreakConfig = new StreakConfig(StreakMode.Daily),
        };
        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        await db.SaveChangesAsync();

        db.Groups.Remove(group);
        await db.SaveChangesAsync();

        var remains = await db.GroupConfigs
            .AsNoTracking()
            .AnyAsync(gc => gc.GroupId == group.Id);
        remains.Should().BeFalse();
    }

    [Fact]
    public async Task GetStreakConfigAsync_returns_the_daily_config_for_global()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var configs = scope.ServiceProvider.GetRequiredService<IGroupConfigService>();

        var config = await configs.GetStreakConfigAsync(GroupIds.Global);

        config.Mode.Should().Be(StreakMode.Daily);
        config.RequiredDaysPerWeek.Should().BeNull();
        config.WeekStart.Should().BeNull();
    }

    [Fact]
    public async Task GetStreakConfigAsync_returns_the_weekly_config_for_a_custom_group()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configs = scope.ServiceProvider.GetRequiredService<IGroupConfigService>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group { Id = Guid.NewGuid(), Name = $"wk_{suffix}", IsRoot = false };
        var config = new GroupConfig
        {
            GroupId = group.Id,
            StreakConfig = new StreakConfig(
                StreakMode.Weekly,
                RequiredDaysPerWeek: 4,
                WeekStart: DayOfWeek.Monday),
        };
        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        await db.SaveChangesAsync();

        var result = await configs.GetStreakConfigAsync(group.Id);

        result.Mode.Should().Be(StreakMode.Weekly);
        result.RequiredDaysPerWeek.Should().Be(4);
        result.WeekStart.Should().Be(DayOfWeek.Monday);
    }

    [Fact]
    public async Task GetStreakConfigAsync_throws_when_the_group_has_no_config_row()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var configs = scope.ServiceProvider.GetRequiredService<IGroupConfigService>();

        var act = async () => await configs.GetStreakConfigAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
