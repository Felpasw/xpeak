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

namespace Xpeak.Api.Tests.CheckIns;

public sealed class GetCheckInEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GetCheckInEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/check_ins/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_check_in_does_not_exist()
    {
        var user = await CreateUserAsync("det_missing");
        var client = AuthenticatedClientFor(user);

        var response = await client.GetAsync($"/check_ins/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_the_check_in_with_the_author_snapshot_and_zero_aggregates()
    {
        var owner = await CreateUserAsync("det_owner");
        var checkInId = await InsertCheckInAsync(owner.Id, "Leg day");
        var client = AuthenticatedClientFor(owner);

        var body = await client.GetFromJsonAsync<DetailBody>($"/check_ins/{checkInId}");

        body.Should().NotBeNull();
        body!.Id.Should().Be(checkInId);
        body.Title.Should().Be("Leg day");
        body.Author.Id.Should().Be(owner.Id);
        body.Author.Username.Should().Be(owner.UserName);
        body.Reactions.Counts.Should().BeEmpty();
        body.Reactions.Mine.Should().BeEmpty();
        body.CommentsCount.Should().Be(0);
    }

    [Fact]
    public async Task Reaction_aggregate_and_comment_count_show_up_inline()
    {
        var owner = await CreateUserAsync("det_o");
        var other = await CreateUserAsync("det_t");
        var checkInId = await InsertCheckInAsync(owner.Id, "Pull day");

        var ownerClient = AuthenticatedClientFor(owner);
        var otherClient = AuthenticatedClientFor(other);

        await ownerClient.PostAsJsonAsync($"/check_ins/{checkInId}/reactions", new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync($"/check_ins/{checkInId}/reactions", new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync($"/check_ins/{checkInId}/reactions", new { emoji = "💪" });
        await otherClient.PostAsJsonAsync($"/check_ins/{checkInId}/comments", new { body = "massa" });
        await otherClient.PostAsJsonAsync($"/check_ins/{checkInId}/comments", new { body = "+1" });

        var body = await ownerClient.GetFromJsonAsync<DetailBody>($"/check_ins/{checkInId}");

        body.Should().NotBeNull();
        body!.Reactions.Counts["🔥"].Should().Be(2);
        body.Reactions.Counts["💪"].Should().Be(1);
        body.Reactions.Mine.Should().BeEquivalentTo(new[] { "🔥" });
        body.CommentsCount.Should().Be(2);
    }

    [Fact]
    public async Task Soft_deleted_comments_do_not_count()
    {
        var owner = await CreateUserAsync("det_sd");
        var checkInId = await InsertCheckInAsync(owner.Id, "Soft delete check");
        var client = AuthenticatedClientFor(owner);

        var create = await client.PostAsJsonAsync($"/check_ins/{checkInId}/comments", new { body = "doomed" });
        var created = await create.Content.ReadFromJsonAsync<CommentCreated>();

        (await client.DeleteAsync($"/comments/{created!.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var body = await client.GetFromJsonAsync<DetailBody>($"/check_ins/{checkInId}");
        body!.CommentsCount.Should().Be(0);
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

    private async Task<Guid> InsertCheckInAsync(Guid userId, string title)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.CheckIns.Add(new CheckIn
        {
            Id = id,
            Title = title,
            UserId = userId,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed record DetailBody(
        Guid Id,
        string Title,
        int XpEarned,
        DateTimeOffset PerformedAt,
        bool HasMedia,
        AuthorBody Author,
        ReactionsBody Reactions,
        int CommentsCount);

    private sealed record AuthorBody(Guid Id, string Username, string? AvatarUrl);

    private sealed record ReactionsBody(
        Dictionary<string, int> Counts,
        List<string> Mine);

    private sealed record CommentCreated(Guid Id);
}
