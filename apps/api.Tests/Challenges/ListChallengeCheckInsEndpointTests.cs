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
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ListChallengeCheckInsEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ListChallengeCheckInsEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/challenges/{Guid.NewGuid()}/check_ins");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_caller_is_not_a_member()
    {
        var owner = await CreateUserAsync("ciowner");
        var outsider = await CreateUserAsync("ciout");
        var challengeId = await CreateChallengeAsync(owner);

        var client = AuthenticatedClientFor(outsider);
        var response = await client.GetAsync($"/challenges/{challengeId}/check_ins");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_member_check_ins_in_the_window_newest_first()
    {
        var owner = await CreateUserAsync("cifeed");
        var challengeId = await CreateChallengeAsync(owner);
        var window = await GetChallengeWindowAsync(challengeId);
        var member = await JoinAsync(challengeId, "cifeedb");
        var outsider = await CreateUserAsync("cifeedout");

        var old = window.StartsAt.AddHours(2);
        var mid = window.StartsAt.AddDays(3);
        var newest = window.EndsAt.AddHours(-2);
        await InsertCheckInAsync(owner.Id, "Owner A", 50, old);
        await InsertCheckInAsync(member.Id, "Member A", 70, mid);
        await InsertCheckInAsync(owner.Id, "Owner B", 60, newest);
        // Noise rows that must not appear in the feed.
        await InsertCheckInAsync(owner.Id, "Before window", 80, window.StartsAt.AddHours(-5));
        await InsertCheckInAsync(outsider.Id, "Not a member", 90, mid);

        var client = AuthenticatedClientFor(owner);
        var response = await client.GetAsync($"/challenges/{challengeId}/check_ins");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<FeedBody>();
        body!.Items.Should().HaveCount(3);
        body.Items[0].CheckIn.Title.Should().Be("Owner B");
        body.Items[0].Author.Username.Should().Be(owner.UserName);
        body.Items[1].CheckIn.Title.Should().Be("Member A");
        body.Items[1].Author.Username.Should().Be(member.UserName);
        body.Items[2].CheckIn.Title.Should().Be("Owner A");
        body.Items[2].Author.Username.Should().Be(owner.UserName);
        body.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task Paginates_through_the_cursor_without_overlap()
    {
        var owner = await CreateUserAsync("cipage");
        var challengeId = await CreateChallengeAsync(owner);
        var window = await GetChallengeWindowAsync(challengeId);

        var titles = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var title = $"CheckIn {i}";
            titles.Add(title);
            await InsertCheckInAsync(owner.Id, title, 10, window.StartsAt.AddHours(i + 1));
        }

        var client = AuthenticatedClientFor(owner);
        var first = await client.GetFromJsonAsync<FeedBody>(
            $"/challenges/{challengeId}/check_ins?limit=2");
        first!.Items.Should().HaveCount(2);
        first.NextCursor.Should().NotBeNullOrEmpty();

        var second = await client.GetFromJsonAsync<FeedBody>(
            $"/challenges/{challengeId}/check_ins?limit=2&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        second!.Items.Should().HaveCount(2);
        second.NextCursor.Should().NotBeNullOrEmpty();

        var third = await client.GetFromJsonAsync<FeedBody>(
            $"/challenges/{challengeId}/check_ins?limit=2&cursor={Uri.EscapeDataString(second.NextCursor!)}");
        third!.Items.Should().HaveCount(1);
        third.NextCursor.Should().BeNull();

        var seen = first.Items
            .Concat(second.Items)
            .Concat(third.Items)
            .Select(i => i.CheckIn.Title)
            .ToList();
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().Equal(titles.AsEnumerable().Reverse());
    }

    private async Task<(DateTimeOffset StartsAt, DateTimeOffset EndsAt)> GetChallengeWindowAsync(Guid challengeId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Challenges
            .FindAsync(challengeId);
        return (row!.StartsAt, row.EndsAt);
    }

    private async Task<AppUser> JoinAsync(Guid challengeId, string prefix)
    {
        var user = await CreateUserAsync(prefix);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.ChallengeMemberships.Add(new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            UserId = user.Id,
            Role = ChallengeMembershipRole.Member,
        });
        await db.SaveChangesAsync();
        return user;
    }

    private async Task InsertCheckInAsync(
        Guid userId,
        string title,
        int xp,
        DateTimeOffset performedAt)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = title,
            UserId = userId,
            XpEarned = xp,
            ScoringSnapshot = new ScoringSnapshot(xp, [], xp),
            PerformedAt = performedAt,
        });
        await db.SaveChangesAsync();
    }

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            token.Value);
        return client;
    }

    private async Task<Guid> CreateChallengeAsync(AppUser owner)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(owner.Id, new(
            Title: "feed",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: 10,
            XpMultiplier: 1.0m,
            StartsAt: now.AddDays(-1),
            EndsAt: now.AddDays(30),
            MemberCap: null));
        return result.Challenge.Id;
    }

    private async Task<AppUser> CreateUserAsync(string prefix)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser
        {
            UserName = $"{prefix}_{suffix}",
            Email = $"{prefix}_{suffix}@x.com",
        };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private sealed record FeedBody(List<FeedItem> Items, string? NextCursor);

    private sealed record FeedItem(CheckInBlock CheckIn, AuthorBlock Author);

    private sealed record CheckInBlock(Guid Id, string Title, int XpEarned, DateTimeOffset PerformedAt);

    private sealed record AuthorBlock(Guid Id, string Username);
}
