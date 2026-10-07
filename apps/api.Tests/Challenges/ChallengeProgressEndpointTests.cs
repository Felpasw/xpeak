using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeProgressEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengeProgressEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Returns_progress_payload_for_a_member()
    {
        var fixture = await SeedFixtureAsync();
        var challengeId = await CreateChallengeAsync(fixture, targetLevel: 5);
        await InsertCheckInAsync(fixture, xp: 50);

        var client = AuthenticatedClientFor(fixture.User);
        var response = await client.GetAsync($"/challenges/{challengeId}/progress");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ProgressBody>();
        body!.Progress.Value.Should().Be(50);
        body.Progress.Target.Should().Be(LevelCurve.XpForLevel(5));
        body.Progress.AchievedAt.Should().BeNull();
    }

    [Fact]
    public async Task Returns_404_when_caller_is_not_a_member()
    {
        var owner = await SeedFixtureAsync();
        var outsider = await SeedFixtureAsync();
        var challengeId = await CreateChallengeAsync(owner, targetLevel: 5);

        var client = AuthenticatedClientFor(outsider.User);
        var response = await client.GetAsync($"/challenges/{challengeId}/progress");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<Guid> CreateChallengeAsync(SeededFixture fixture, int targetLevel)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(fixture.User.Id, new(
            Title: "p",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: targetLevel,
            XpMultiplier: 1.0m,
            StartsAt: now.AddDays(-1),
            EndsAt: now.AddDays(30),
            MemberCap: null,
            CategoryIds: new[] { fixture.Category.Id }));
        return result.Challenge.Id;
    }

    private async Task InsertCheckInAsync(SeededFixture fixture, int xp)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "t",
            UserId = fixture.User.Id,
            CategoryId = fixture.Category.Id,
            GroupId = fixture.Group.Id,
            XpEarned = xp,
            ScoringSnapshot = new ScoringSnapshot(xp, [], xp),
            PerformedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"cp_{suffix}", Email = $"cp_{suffix}@x.com" };
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

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private sealed record ProgressBody(ProgressBlock Progress);

    private sealed record ProgressBlock(int Value, int Target, DateTimeOffset? AchievedAt);
}
