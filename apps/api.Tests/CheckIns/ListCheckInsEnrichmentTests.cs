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

public sealed class ListCheckInsEnrichmentTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ListCheckInsEnrichmentTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task List_rows_carry_the_reactions_aggregate_and_live_comment_count()
    {
        var owner = await CreateUserAsync("enr_o");
        var other = await CreateUserAsync("enr_t");

        var engagedId = await InsertCheckInAsync(owner.Id, DateTimeOffset.UtcNow);
        var emptyId = await InsertCheckInAsync(
            owner.Id, DateTimeOffset.UtcNow.AddMinutes(-5));

        var ownerClient = AuthenticatedClientFor(owner);
        var otherClient = AuthenticatedClientFor(other);

        await ownerClient.PostAsJsonAsync(
            $"/check_ins/{engagedId}/reactions",
            new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{engagedId}/reactions",
            new { emoji = "🔥" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{engagedId}/reactions",
            new { emoji = "💪" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{engagedId}/comments",
            new { body = "ótimo treino" });
        await otherClient.PostAsJsonAsync(
            $"/check_ins/{engagedId}/comments",
            new { body = "massa" });

        var body = await ownerClient.GetFromJsonAsync<ListBody>("/check_ins?limit=10");

        body.Should().NotBeNull();
        body!.CheckIns.Should().HaveCount(2);

        var engaged = body.CheckIns.Single(c => c.Id == engagedId);
        engaged.Reactions.Counts["🔥"].Should().Be(2);
        engaged.Reactions.Counts["💪"].Should().Be(1);
        engaged.Reactions.Mine.Should().BeEquivalentTo(new[] { "🔥" });
        engaged.CommentsCount.Should().Be(2);

        var empty = body.CheckIns.Single(c => c.Id == emptyId);
        empty.Reactions.Counts.Should().BeEmpty();
        empty.Reactions.Mine.Should().BeEmpty();
        empty.CommentsCount.Should().Be(0);
    }

    [Fact]
    public async Task Soft_deleted_comments_drop_out_of_the_list_badge()
    {
        var owner = await CreateUserAsync("enr_sd");
        var checkInId = await InsertCheckInAsync(owner.Id, DateTimeOffset.UtcNow);
        var client = AuthenticatedClientFor(owner);

        var created = await (await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "temp" })).Content.ReadFromJsonAsync<CommentCreated>();
        await client.DeleteAsync($"/comments/{created!.Id}");

        var body = await client.GetFromJsonAsync<ListBody>("/check_ins?limit=10");
        var row = body!.CheckIns.Single(c => c.Id == checkInId);
        row.CommentsCount.Should().Be(0);
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

    private async Task<Guid> InsertCheckInAsync(Guid userId, DateTimeOffset at)
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
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
        return id;
    }

    private sealed record ListBody(List<RowBody> CheckIns, string? NextCursor);

    private sealed record RowBody(
        Guid Id,
        string Title,
        int XpEarned,
        ReactionsBody Reactions,
        int CommentsCount);

    private sealed record ReactionsBody(
        Dictionary<string, int> Counts,
        List<string> Mine);

    private sealed record CommentCreated(Guid Id);
}
