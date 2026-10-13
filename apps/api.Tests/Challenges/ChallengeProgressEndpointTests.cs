using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups;
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
        var user = await CreateUserAsync("cp");
        var categoryId = await EnsureCategoryAsync("cp-any");
        var challengeId = await CreateChallengeAsync(user, targetLevel: 5);
        await InsertCheckInAsync(user.Id, categoryId, xp: 50);

        var client = AuthenticatedClientFor(user);
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
        var owner = await CreateUserAsync("cpowner");
        var outsider = await CreateUserAsync("cpoutsider");
        var challengeId = await CreateChallengeAsync(owner, targetLevel: 5);

        var client = AuthenticatedClientFor(outsider);
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

    private async Task<Guid> CreateChallengeAsync(AppUser owner, int targetLevel)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(owner.Id, new(
            Title: "p",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: targetLevel,
            XpMultiplier: 1.0m,
            StartsAt: now.AddDays(-1),
            EndsAt: now.AddDays(30),
            MemberCap: null));
        return result.Challenge.Id;
    }

    private async Task<Guid> EnsureCategoryAsync(string slug)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var defaultXpRuleId = new Guid("00000000-0000-0000-0000-000000000010");
        var fullSlug = $"{slug}-{Guid.NewGuid():N}";
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = defaultXpRuleId,
            Slug = fullSlug,
            Name = fullSlug,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
    }

    private async Task InsertCheckInAsync(Guid userId, Guid categoryId, int xp)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "t",
            UserId = userId,
            CategoryId = categoryId,
            GroupId = GroupIds.Global,
            XpEarned = xp,
            ScoringSnapshot = new ScoringSnapshot(xp, [], xp),
            PerformedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task<AppUser> CreateUserAsync(string prefix)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"{prefix}_{suffix}", Email = $"{prefix}_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private sealed record ProgressBody(ProgressBlock Progress);

    private sealed record ProgressBlock(int Value, int Target, DateTimeOffset? AchievedAt);
}
