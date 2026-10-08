using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengesEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengesEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Create_rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/challenges", new { });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_happy_path_returns_201_with_payload()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "30 dias",
            description = "legs only",
            iconPublicId = (string?)null,
            visibility = "private",
            targetLevel = 5,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(30),
            memberCap = (int?)null,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateBody>();
        body.Should().NotBeNull();
        body!.Challenge.Title.Should().Be("30 dias");
        body.Challenge.TargetLevel.Should().Be(5);
        body.OwnerMembership.Role.Should().Be("owner");
    }

    [Fact]
    public async Task Create_rejects_blank_title_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "   ",
            visibility = "private",
            targetLevel = 5,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(30),
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task List_returns_only_the_callers_memberships()
    {
        var mine = await SeedFixtureAsync();
        var others = await SeedFixtureAsync();

        await CreateChallengeDirectAsync(mine, "mine");
        await CreateChallengeDirectAsync(others, "theirs");

        var client = AuthenticatedClientFor(mine.User);
        var response = await client.GetFromJsonAsync<ListBody>("/challenges");

        response.Should().NotBeNull();
        response!.Challenges.Should().ContainSingle().Which.Title.Should().Be("mine");
    }

    [Fact]
    public async Task List_filters_by_state_query_param()
    {
        var fixture = await SeedFixtureAsync();
        var active = await CreateChallengeDirectAsync(fixture, "active");
        var completed = await CreateChallengeDirectAsync(fixture, "completed");
        await SetStateAsync(completed, ChallengeState.Completed);

        var client = AuthenticatedClientFor(fixture.User);
        var activeOnly = await client.GetFromJsonAsync<ListBody>("/challenges?state=active");
        var completedOnly = await client.GetFromJsonAsync<ListBody>("/challenges?state=completed");

        activeOnly!.Challenges.Should().ContainSingle().Which.Id.Should().Be(active);
        completedOnly!.Challenges.Should().ContainSingle().Which.Id.Should().Be(completed);
    }

    [Fact]
    public async Task Get_detail_returns_200_for_member()
    {
        var fixture = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(fixture, "visible");

        var client = AuthenticatedClientFor(fixture.User);
        var response = await client.GetAsync($"/challenges/{challenge}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DetailBody>();
        body!.Challenge.Id.Should().Be(challenge);
        body.Membership.Should().NotBeNull();
    }

    [Fact]
    public async Task Get_detail_returns_404_for_non_member_private()
    {
        var owner = await SeedFixtureAsync();
        var outsider = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "private");

        var client = AuthenticatedClientFor(outsider.User);
        var response = await client.GetAsync($"/challenges/{challenge}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_detail_returns_200_for_non_member_on_public()
    {
        var owner = await SeedFixtureAsync();
        var outsider = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "public");
        await SetVisibilityAsync(challenge, ChallengeVisibility.Public);

        var client = AuthenticatedClientFor(outsider.User);
        var response = await client.GetAsync($"/challenges/{challenge}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DetailBody>();
        body!.Membership.Should().BeNull();
    }

    [Fact]
    public async Task Delete_204_for_owner()
    {
        var fixture = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(fixture, "to-delete");

        var client = AuthenticatedClientFor(fixture.User);
        var response = await client.DeleteAsync($"/challenges/{challenge}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_404_for_non_owner()
    {
        var owner = await SeedFixtureAsync();
        var outsider = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "not-yours");

        var client = AuthenticatedClientFor(outsider.User);
        var response = await client.DeleteAsync($"/challenges/{challenge}");

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

    private async Task<Guid> CreateChallengeDirectAsync(SeededFixture fixture, string title)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<Xpeak.Api.Challenges.Services.IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(fixture.User.Id, new(
            Title: title,
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: 5,
            XpMultiplier: 1.0m,
            StartsAt: now,
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

    private async Task SetVisibilityAsync(Guid challengeId, ChallengeVisibility visibility)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Challenges.SingleAsync(c => c.Id == challengeId);
        row.Visibility = visibility;
        await db.SaveChangesAsync();
    }

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ce_{suffix}", Email = $"ce_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        return new SeededFixture(user);
    }

    private sealed record SeededFixture(AppUser User);

    private sealed record CreateBody(ChallengeBody Challenge, MembershipBody OwnerMembership);

    private sealed record ListBody(IReadOnlyList<ChallengeBody> Challenges);

    private sealed record DetailBody(ChallengeBody Challenge, MembershipBody? Membership);

    private sealed record ChallengeBody(
        Guid Id,
        string Title,
        int TargetLevel,
        string Visibility,
        string State);

    private sealed record MembershipBody(string Role);
}
