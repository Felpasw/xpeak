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
        body.ShareLink.Id.Should().NotBeEmpty();
        body.ShareLink.ChallengeId.Should().Be(body.Challenge.Id);
        body.ShareLink.State.Should().Be("pending");
        body.Tiers.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_with_tiers_returns_201_and_echoes_them_sorted()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "100 dias",
            visibility = "private",
            targetLevel = 60,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(100),
            memberCap = (int?)null,
            tiers = new[]
            {
                new { minLevel = 60, title = "ArchMago" },
                new { minLevel = 10, title = " Aprendiz " },
                new { minLevel = 30, title = "Mago" },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateBody>();
        body!.Tiers.Should().HaveCount(3);
        body.Tiers.Select(t => t.MinLevel).Should().ContainInOrder(10, 30, 60);
        body.Tiers.Select(t => t.Title).Should().ContainInOrder("Aprendiz", "Mago", "ArchMago");
    }

    [Fact]
    public async Task Create_rejects_eleven_tiers_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var tiers = Enumerable.Range(1, 11)
            .Select(i => new { minLevel = i, title = $"Tier {i}" })
            .ToArray();

        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "overflow",
            visibility = "private",
            targetLevel = 20,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(30),
            memberCap = (int?)null,
            tiers,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_rejects_duplicate_min_level_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "dup",
            visibility = "private",
            targetLevel = 20,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(30),
            memberCap = (int?)null,
            tiers = new[]
            {
                new { minLevel = 10, title = "Aprendiz" },
                new { minLevel = 10, title = "Outro" },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_rejects_blank_tier_title_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var now = DateTimeOffset.UtcNow;
        var response = await client.PostAsJsonAsync("/challenges", new
        {
            title = "blank-tier",
            visibility = "private",
            targetLevel = 20,
            xpMultiplier = 1.0m,
            startsAt = now,
            endsAt = now.AddDays(30),
            memberCap = (int?)null,
            tiers = new[]
            {
                new { minLevel = 10, title = "   " },
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
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
        body.ShareLink.Should().NotBeNull();
        body.ShareLink!.ChallengeId.Should().Be(challenge);
        body.Tiers.Should().BeEmpty();
        body.CurrentTier.Should().BeNull();
    }

    [Fact]
    public async Task Get_detail_returns_tiers_and_current_tier_for_a_member_with_progress()
    {
        var fixture = await SeedFixtureAsync();
        var challengeId = await CreateChallengeWithTiersAsync(fixture, "with-tiers",
        [
            new(10, "Aprendiz"),
            new(30, "Mago"),
            new(60, "ArchMago"),
        ]);
        await SetProgressAsync(challengeId, fixture.User.Id, value: 30);

        var client = AuthenticatedClientFor(fixture.User);
        var response = await client.GetAsync($"/challenges/{challengeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<DetailBody>();
        body!.Tiers.Select(t => t.MinLevel).Should().ContainInOrder(10, 30, 60);
        body.CurrentTier.Should().NotBeNull();
        body.CurrentTier!.MinLevel.Should().Be(30);
        body.CurrentTier.Title.Should().Be("Mago");
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
        body.ShareLink.Should().BeNull();
        body.Tiers.Should().BeEmpty();
        body.CurrentTier.Should().BeNull();
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

    [Fact]
    public async Task Join_rejects_an_unknown_invitation_with_404()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            "/challenges/join",
            new { invitationId = Guid.NewGuid() });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Join_rejects_an_empty_invitation_id_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            "/challenges/join",
            new { invitationId = Guid.Empty });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Join_happy_path_returns_200_with_payload()
    {
        var owner = await SeedFixtureAsync();
        var joiner = await SeedFixtureAsync();
        var invitationId = await CreateChallengeAndGetShareLinkAsync(owner, "join-me");

        var client = AuthenticatedClientFor(joiner.User);
        var response = await client.PostAsJsonAsync(
            "/challenges/join",
            new { invitationId });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JoinBody>();
        body.Should().NotBeNull();
        body!.Membership.Role.Should().Be("member");
    }

    [Fact]
    public async Task Join_rejects_a_double_join_with_409()
    {
        var owner = await SeedFixtureAsync();
        var invitationId = await CreateChallengeAndGetShareLinkAsync(owner, "owner-rejoin");
        var client = AuthenticatedClientFor(owner.User);

        var response = await client.PostAsJsonAsync(
            "/challenges/join",
            new { invitationId });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task End_happy_path_returns_204()
    {
        var owner = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "to-end");
        var client = AuthenticatedClientFor(owner.User);

        var response = await client.PostAsync($"/challenges/{challenge}/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task End_hides_the_challenge_from_non_owners_with_404()
    {
        var owner = await SeedFixtureAsync();
        var outsider = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "not-yours-end");
        var client = AuthenticatedClientFor(outsider.User);

        var response = await client.PostAsync($"/challenges/{challenge}/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task End_rejects_an_already_ended_challenge_with_409()
    {
        var owner = await SeedFixtureAsync();
        var challenge = await CreateChallengeDirectAsync(owner, "already-ended");
        await SetStateAsync(challenge, ChallengeState.Completed);
        var client = AuthenticatedClientFor(owner.User);

        var response = await client.PostAsync($"/challenges/{challenge}/end", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

    private async Task<Guid> CreateChallengeAndGetShareLinkAsync(SeededFixture fixture, string title)
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
        return result.ShareLink.Id;
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

    private async Task<Guid> CreateChallengeWithTiersAsync(
        SeededFixture fixture,
        string title,
        IReadOnlyList<Xpeak.Api.Challenges.Dto.CreateChallengeTierInput> tiers)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<Xpeak.Api.Challenges.Services.IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(fixture.User.Id, new(
            Title: title,
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: 100,
            XpMultiplier: 1.0m,
            StartsAt: now,
            EndsAt: now.AddDays(100),
            MemberCap: null,
            Tiers: tiers));
        return result.Challenge.Id;
    }

    private async Task SetProgressAsync(Guid challengeId, Guid userId, int value)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var membership = await db.ChallengeMemberships
            .SingleAsync(m => m.ChallengeId == challengeId && m.UserId == userId);
        var challenge = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == challengeId);
        membership.Progress = new Xpeak.Api.Challenges.Entities.MembershipProgress(
            Value: value,
            Target: challenge.TargetLevel,
            AchievedAt: null);
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

    private sealed record CreateBody(
        ChallengeBody Challenge,
        MembershipBody OwnerMembership,
        ShareLinkBody ShareLink,
        IReadOnlyList<TierBody> Tiers);

    private sealed record ListBody(IReadOnlyList<ChallengeBody> Challenges);

    private sealed record DetailBody(
        ChallengeBody Challenge,
        MembershipBody? Membership,
        ShareLinkBody? ShareLink,
        IReadOnlyList<TierBody> Tiers,
        TierBody? CurrentTier);

    private sealed record ChallengeBody(
        Guid Id,
        string Title,
        int TargetLevel,
        string Visibility,
        string State);

    private sealed record MembershipBody(string Role);

    private sealed record ShareLinkBody(Guid Id, Guid ChallengeId, string State);

    private sealed record TierBody(Guid Id, int MinLevel, string Title);

    private sealed record JoinBody(ChallengeBody Challenge, MembershipBody Membership);
}
