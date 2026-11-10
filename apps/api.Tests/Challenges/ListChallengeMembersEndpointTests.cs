using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ListChallengeMembersEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ListChallengeMembersEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/challenges/{Guid.NewGuid()}/members");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_caller_is_not_a_member()
    {
        var owner = await CreateUserAsync("lmowner");
        var outsider = await CreateUserAsync("lmout");
        var challengeId = await CreateChallengeAsync(owner);

        var client = AuthenticatedClientFor(outsider);
        var response = await client.GetAsync($"/challenges/{challengeId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_members_ordered_by_progress_descending()
    {
        var owner = await CreateUserAsync("lmrank");
        var challengeId = await CreateChallengeAsync(owner);
        var low = await JoinAsync(challengeId, "lmlow", progress: 20);
        var high = await JoinAsync(challengeId, "lmhigh", progress: 120);
        var mid = await JoinAsync(challengeId, "lmmid", progress: 70);

        var client = AuthenticatedClientFor(owner);
        var response = await client.GetAsync($"/challenges/{challengeId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MembersBody>();
        body!.Members.Should().HaveCount(4);
        // Owner joined with 0 progress; the three members we seeded take
        // the ranked slots 1..3 ordered by their XP.
        body.Members[0].User.Username.Should().Be(high.UserName);
        body.Members[0].Progress.Value.Should().Be(120);
        body.Members[1].User.Username.Should().Be(mid.UserName);
        body.Members[1].Progress.Value.Should().Be(70);
        body.Members[2].User.Username.Should().Be(low.UserName);
        body.Members[2].Progress.Value.Should().Be(20);
        body.Members[3].User.Username.Should().Be(owner.UserName);
        body.Members[3].Progress.Value.Should().Be(0);
    }

    [Fact]
    public async Task Breaks_identical_progress_ties_by_user_id_ascending()
    {
        var owner = await CreateUserAsync("lmties");
        var challengeId = await CreateChallengeAsync(owner);
        var fixedProgress = 50;
        var members = new List<AppUser>
        {
            await JoinAsync(challengeId, "lmtiea", progress: fixedProgress),
            await JoinAsync(challengeId, "lmtieb", progress: fixedProgress),
            await JoinAsync(challengeId, "lmtiec", progress: fixedProgress),
        };
        var expected = members
            .Select(m => m.UserName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        var client = AuthenticatedClientFor(owner);
        var response = await client.GetAsync($"/challenges/{challengeId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MembersBody>();
        var tied = body!.Members
            .Where(m => m.Progress.Value == fixedProgress)
            .Select(m => m.User.Username)
            .ToList();

        // We cannot assume the User.Id order matches the username order
        // (ids are random guids), so we assert the tied group is sorted by
        // the key the endpoint promises: UserId ascending. Resolve the
        // expected id order from the DB and compare against the response.
        var expectedByIdOrder = await ResolveExpectedOrderByUserId(
            challengeId,
            fixedProgress);
        tied.Should().Equal(expectedByIdOrder);
        expected.Should().NotBeNull();
    }

    private async Task<List<string>> ResolveExpectedOrderByUserId(
        Guid challengeId,
        int progressValue)
    {
        // Progress is JSONB; load full entities and filter in memory to
        // avoid any EF projection of the JSON record's inner fields.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var memberships = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.ChallengeId == challengeId)
            .ToListAsync();
        var tied = memberships
            .Where(m => m.Progress.Value == progressValue)
            .OrderBy(m => m.UserId)
            .Select(m => m.UserId)
            .ToList();
        var users = await db.Users
            .AsNoTracking()
            .Where(u => tied.Contains(u.Id))
            .ToListAsync();
        var byId = users.ToDictionary(u => u.Id, u => u.UserName!);
        return tied.Select(id => byId[id]).ToList();
    }

    private async Task<AppUser> JoinAsync(Guid challengeId, string prefix, int progress)
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
            Progress = new MembershipProgress(progress, 100, null),
        });
        await db.SaveChangesAsync();
        return user;
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
            Title: "members",
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

    private sealed record MembersBody(List<MemberRow> Members);

    private sealed record MemberRow(PublicUserBlock User, ProgressBlock Progress);

    private sealed record PublicUserBlock(Guid Id, string Username);

    private sealed record ProgressBlock(int Value, int Target, DateTimeOffset? AchievedAt);
}
