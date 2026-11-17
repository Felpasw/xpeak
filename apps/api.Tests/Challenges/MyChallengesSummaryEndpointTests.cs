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
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Challenges;

public sealed class MyChallengesSummaryEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public MyChallengesSummaryEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/challenges/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_only_active_memberships_with_inline_progress()
    {
        var mine = await CreateUserAsync("sm");
        var others = await CreateUserAsync("so");

        var activeMine = await CreateChallengeAsync(mine, "mine-active", targetLevel: 5);
        var completedMine = await CreateChallengeAsync(mine, "mine-completed", targetLevel: 5);
        await SetStateAsync(completedMine, ChallengeState.Completed);
        await CreateChallengeAsync(others, "theirs", targetLevel: 5);

        await InsertCheckInAsync(mine.Id, xp: 50);

        var client = AuthenticatedClientFor(mine);
        var response = await client.GetFromJsonAsync<SummaryBody>("/challenges/summary");

        response.Should().NotBeNull();
        var summary = response!.Summaries.Should().ContainSingle().Which;
        summary.Id.Should().Be(activeMine);
        summary.Title.Should().Be("mine-active");
        summary.State.Should().Be("active");
        summary.Progress.Value.Should().Be(50);
        summary.Progress.Target.Should().Be(LevelCurve.XpForLevel(5));
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

    private async Task<Guid> CreateChallengeAsync(AppUser owner, string title, int targetLevel)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(owner.Id, new(
            Title: title,
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

    private async Task SetStateAsync(Guid challengeId, ChallengeState state)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Challenges.SingleAsync(c => c.Id == challengeId);
        row.State = state;
        await db.SaveChangesAsync();
    }

    private async Task InsertCheckInAsync(Guid userId, int xp)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "t",
            UserId = userId,
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

    private sealed record SummaryBody(IReadOnlyList<SummaryRow> Summaries);

    private sealed record SummaryRow(
        Guid Id,
        string Title,
        string State,
        ProgressRow Progress);

    private sealed record ProgressRow(int Value, int Target);
}
