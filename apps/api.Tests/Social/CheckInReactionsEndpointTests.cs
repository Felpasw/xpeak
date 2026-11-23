using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Social;

public sealed class CheckInReactionsEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInReactionsEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/reactions",
            new { emoji = "🔥" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_toggles_same_emoji_up_then_down()
    {
        var user = await CreateUserAsync("rx");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var first = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<ReactionsBody>();
        firstBody!.Counts.Should().ContainKey("🔥").WhoseValue.Should().Be(1);
        firstBody.Mine.Should().Contain("🔥");

        var second = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<ReactionsBody>();
        secondBody!.Counts.Should().NotContainKey("🔥");
        secondBody.Mine.Should().NotContain("🔥");
    }

    [Fact]
    public async Task Post_persists_distinct_emojis_from_the_same_user()
    {
        var user = await CreateUserAsync("rx");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        var res = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "💪" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<ReactionsBody>();
        body!.Counts.Should().ContainKey("🔥").WhoseValue.Should().Be(1);
        body.Counts.Should().ContainKey("💪").WhoseValue.Should().Be(1);
        body.Mine.Should().BeEquivalentTo(new[] { "🔥", "💪" });
    }

    [Fact]
    public async Task Post_rejects_an_emoji_outside_the_palette_with_422()
    {
        var user = await CreateUserAsync("rx");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "💩" });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_returns_404_when_check_in_does_not_exist()
    {
        var user = await CreateUserAsync("rx");
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/reactions",
            new { emoji = "🔥" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_returns_the_aggregate_with_caller_slice()
    {
        var owner = await CreateUserAsync("rxo");
        var other = await CreateUserAsync("rxt");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var ownerClient = AuthenticatedClientFor(owner);
        var otherClient = AuthenticatedClientFor(other);

        await ownerClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "💪" });

        var response = await ownerClient.GetFromJsonAsync<ReactionsBody>(
            $"/check_ins/{checkInId}/reactions");

        response.Should().NotBeNull();
        response!.Counts["🔥"].Should().Be(2);
        response.Counts["💪"].Should().Be(1);
        response.Mine.Should().BeEquivalentTo(new[] { "🔥" });
    }

    [Fact]
    public async Task Get_with_include_users_returns_the_grouped_user_list()
    {
        var owner = await CreateUserAsync("rxo");
        var other = await CreateUserAsync("rxt");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var ownerClient = AuthenticatedClientFor(owner);
        var otherClient = AuthenticatedClientFor(other);

        await ownerClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/reactions",
            new { emoji = "🔥" });

        var response = await ownerClient.GetFromJsonAsync<ReactionsWithUsersBody>(
            $"/check_ins/{checkInId}/reactions?include=users");

        response.Should().NotBeNull();
        response!.Users["🔥"].Should().HaveCount(2);
        response.Users["🔥"].Select(u => u.Id)
            .Should().BeEquivalentTo(new[] { owner.Id, other.Id });
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

    private async Task<AppUser> CreateUserAsync(string prefix)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"{prefix}_{suffix}", Email = $"{prefix}_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<Guid> InsertCheckInAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.CheckIns.Add(new CheckIn
        {
            Id = id,
            Title = "t",
            UserId = userId,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed record ReactionsBody(
        Dictionary<string, int> Counts,
        List<string> Mine);

    private sealed record ReactionsWithUsersBody(
        Dictionary<string, int> Counts,
        List<string> Mine,
        Dictionary<string, List<UserBody>> Users);

    private sealed record UserBody(Guid Id, string Username, string? AvatarUrl);
}
