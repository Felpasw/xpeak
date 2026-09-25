using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class ListCheckInsEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ListCheckInsEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/check_ins");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_only_the_callers_rows_ordered_newest_first()
    {
        var mine = await SeedFixtureAsync();
        var others = await SeedFixtureAsync();
        var now = DateTimeOffset.UtcNow;

        await InsertAsync(mine, now.AddDays(-2));
        await InsertAsync(mine, now.AddDays(-1));
        await InsertAsync(mine, now);
        await InsertAsync(others, now);

        var client = AuthenticatedClientFor(mine.User);

        var response = await client.GetAsync("/check_ins?limit=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListBody>();
        body.Should().NotBeNull();
        body!.CheckIns.Should().HaveCount(3);
        body.CheckIns.Should().BeInDescendingOrder(c => c.PerformedAt);
        body.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Cursor_round_trips_across_pages_without_overlap()
    {
        var fixture = await SeedFixtureAsync();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            await InsertAsync(fixture, now.AddDays(-i));
        }

        var client = AuthenticatedClientFor(fixture.User);

        var page1 = await client.GetFromJsonAsync<ListBody>("/check_ins?limit=2");
        page1!.CheckIns.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNull();

        var page2 = await client.GetFromJsonAsync<ListBody>($"/check_ins?limit=2&cursor={Uri.EscapeDataString(page1.NextCursor!)}");
        page2!.CheckIns.Should().HaveCount(2);
        page2.NextCursor.Should().NotBeNull();

        var page3 = await client.GetFromJsonAsync<ListBody>($"/check_ins?limit=2&cursor={Uri.EscapeDataString(page2.NextCursor!)}");
        page3!.CheckIns.Should().HaveCount(1);
        page3.NextCursor.Should().BeNull();

        var ids = page1.CheckIns.Concat(page2.CheckIns).Concat(page3.CheckIns).Select(c => c.Id).ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().HaveCount(5);
    }

    [Fact]
    public async Task Group_id_query_param_narrows_the_result()
    {
        var mine = await SeedFixtureAsync();
        var second = await SeedSecondGroupAsync(mine);
        var now = DateTimeOffset.UtcNow;

        await InsertAsync(mine, now);
        await InsertAsync(mine with { Group = second.Group, Category = second.Category }, now);

        var client = AuthenticatedClientFor(mine.User);

        var firstOnly = await client.GetFromJsonAsync<ListBody>($"/check_ins?group_id={mine.Group.Id}");
        var secondOnly = await client.GetFromJsonAsync<ListBody>($"/check_ins?group_id={second.Group.Id}");

        firstOnly!.CheckIns.Should().ContainSingle().Which.GroupId.Should().Be(mine.Group.Id);
        secondOnly!.CheckIns.Should().ContainSingle().Which.GroupId.Should().Be(second.Group.Id);
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private sealed record ListBody(IReadOnlyList<CheckInBody> CheckIns, string? NextCursor);

    private sealed record CheckInBody(
        Guid Id,
        Guid CategoryId,
        Guid GroupId,
        int XpEarned,
        DateTimeOffset PerformedAt);

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"lep_{suffix}", Email = $"lep_{suffix}@x.com" };
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

    private async Task InsertAsync(SeededFixture fixture, DateTimeOffset at)
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
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
    }
}
