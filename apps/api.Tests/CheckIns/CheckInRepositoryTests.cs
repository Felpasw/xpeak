using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Repositories;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CheckInRepositoryTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInRepositoryTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task ListAsync_returns_only_the_callers_rows_ordered_newest_first()
    {
        var mine = await SeedFixtureAsync();
        var others = await SeedFixtureAsync();

        var now = DateTimeOffset.UtcNow;
        await InsertAsync(mine, now.AddDays(-2));
        await InsertAsync(mine, now.AddDays(-1));
        await InsertAsync(mine, now);
        await InsertAsync(others, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page = await repo.ListAsync(mine.User.Id, limit: 10, cursor: null, groupId: null);

        page.Items.Should().HaveCount(3);
        page.Items.Select(c => c.UserId).Should().OnlyContain(id => id == mine.User.Id);
        page.Items.Should().BeInDescendingOrder(c => c.PerformedAt);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_paginates_via_cursor_without_overlap()
    {
        var fixture = await SeedFixtureAsync();
        var now = DateTimeOffset.UtcNow;

        var inserted = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var id = await InsertAsync(fixture, now.AddDays(-i));
            inserted.Add(id);
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page1 = await repo.ListAsync(fixture.User.Id, limit: 2, cursor: null, groupId: null);
        page1.Items.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNull();

        var page2 = await repo.ListAsync(fixture.User.Id, limit: 2, cursor: page1.NextCursor, groupId: null);
        page2.Items.Should().HaveCount(2);
        page2.NextCursor.Should().NotBeNull();

        var page3 = await repo.ListAsync(fixture.User.Id, limit: 2, cursor: page2.NextCursor, groupId: null);
        page3.Items.Should().HaveCount(1);
        page3.NextCursor.Should().BeNull();

        var seen = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(c => c.Id).ToList();
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().HaveCount(5);
        seen.Should().BeEquivalentTo(inserted);
    }

    [Fact]
    public async Task ListAsync_filters_by_group_when_requested()
    {
        var mine = await SeedFixtureAsync();
        var second = await SeedSecondGroupAsync(mine);

        var now = DateTimeOffset.UtcNow;
        await InsertAsync(mine, now);
        await InsertAsync(mine with { Group = second.Group, Category = second.Category }, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var firstGroupOnly = await repo.ListAsync(mine.User.Id, limit: 10, cursor: null, groupId: mine.Group.Id);
        var secondGroupOnly = await repo.ListAsync(mine.User.Id, limit: 10, cursor: null, groupId: second.Group.Id);

        firstGroupOnly.Items.Should().ContainSingle().Which.GroupId.Should().Be(mine.Group.Id);
        secondGroupOnly.Items.Should().ContainSingle().Which.GroupId.Should().Be(second.Group.Id);
    }

    [Fact]
    public async Task ListAsync_returns_empty_page_when_user_has_no_check_ins()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page = await repo.ListAsync(fixture.User.Id, limit: 10, cursor: null, groupId: null);

        page.Items.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"lst_{suffix}", Email = $"lst_{suffix}@x.com" };
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

    private async Task<Guid> InsertAsync(SeededFixture fixture, DateTimeOffset at)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        db.CheckIns.Add(new CheckIn
        {
            Id = id,
            UserId = fixture.User.Id,
            CategoryId = fixture.Category.Id,
            GroupId = fixture.Group.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(
                10,
                [new ScoringMultiplier("category_weight", 1.00m)],
                10),
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
        return id;
    }
}
