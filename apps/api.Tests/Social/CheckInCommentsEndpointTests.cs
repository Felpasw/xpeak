using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Social;

public sealed class CheckInCommentsEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInCommentsEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Post_rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/comments",
            new { body = "gg" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Post_creates_a_comment_and_returns_it_with_the_author()
    {
        var user = await CreateUserAsync("cmt");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "gg mano" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CommentBody>();
        body!.Body.Should().Be("gg mano");
        body.Author.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Post_returns_404_when_check_in_does_not_exist()
    {
        var user = await CreateUserAsync("cmt");
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/comments",
            new { body = "gg" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Post_rejects_an_empty_body_with_422()
    {
        var user = await CreateUserAsync("cmt");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "   " });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_rejects_a_body_over_280_chars_with_422()
    {
        var user = await CreateUserAsync("cmt");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var tooLong = new string('a', 281);
        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = tooLong });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Get_returns_paginated_oldest_first_and_excludes_soft_deleted()
    {
        var owner = await CreateUserAsync("cmt_owner");
        var author = await CreateUserAsync("cmt_author");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var authorClient = AuthenticatedClientFor(author);

        for (var i = 0; i < 3; i++)
        {
            await authorClient.PostAsJsonAsync(
                $"/check_ins/{checkInId}/comments",
                new { body = $"c{i}" });
        }

        var secondId = await FindCommentIdByBodyAsync("c1");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var row = await db.CheckInComments.SingleAsync(c => c.Id == secondId);
            row.DeletedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
        }

        var ownerClient = AuthenticatedClientFor(owner);
        var page1 = await ownerClient.GetFromJsonAsync<PageBody>(
            $"/check_ins/{checkInId}/comments?limit=10");

        page1!.Items.Select(i => i.Body).Should().BeEquivalentTo(
            new[] { "c0", "c2" },
            o => o.WithStrictOrdering());
    }

    [Fact]
    public async Task Delete_allows_the_author()
    {
        var owner = await CreateUserAsync("cmt_o");
        var author = await CreateUserAsync("cmt_a");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var authorClient = AuthenticatedClientFor(author);
        var create = await authorClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "author speaking" });
        var created = await create.Content.ReadFromJsonAsync<CommentBody>();

        var delete = await authorClient.DeleteAsync($"/comments/{created!.Id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_allows_the_check_in_owner()
    {
        var owner = await CreateUserAsync("cmt_o");
        var author = await CreateUserAsync("cmt_a");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var authorClient = AuthenticatedClientFor(author);
        var create = await authorClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "author speaking" });
        var created = await create.Content.ReadFromJsonAsync<CommentBody>();

        var ownerClient = AuthenticatedClientFor(owner);
        var delete = await ownerClient.DeleteAsync($"/comments/{created!.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_rejects_a_third_party_with_403()
    {
        var owner = await CreateUserAsync("cmt_o");
        var author = await CreateUserAsync("cmt_a");
        var stranger = await CreateUserAsync("cmt_s");
        var checkInId = await InsertCheckInAsync(owner.Id);

        var authorClient = AuthenticatedClientFor(author);
        var create = await authorClient.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "author speaking" });
        var created = await create.Content.ReadFromJsonAsync<CommentBody>();

        var strangerClient = AuthenticatedClientFor(stranger);
        var delete = await strangerClient.DeleteAsync($"/comments/{created!.Id}");

        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_returns_404_for_an_already_deleted_comment()
    {
        var user = await CreateUserAsync("cmt_d");
        var checkInId = await InsertCheckInAsync(user.Id);
        var client = AuthenticatedClientFor(user);

        var create = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/comments",
            new { body = "delete me" });
        var created = await create.Content.ReadFromJsonAsync<CommentBody>();

        (await client.DeleteAsync($"/comments/{created!.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.DeleteAsync($"/comments/{created.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private async Task<Guid> FindCommentIdByBodyAsync(string body)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.CheckInComments
            .Where(c => c.Body == body)
            .Select(c => c.Id)
            .SingleAsync();
    }

    private sealed record CommentBody(
        Guid Id,
        Guid CheckInId,
        AuthorBody Author,
        string Body,
        DateTimeOffset CreatedAt);

    private sealed record AuthorBody(Guid Id, string Username, string? AvatarUrl);

    private sealed record PageBody(
        List<CommentBody> Items,
        string? NextCursor);
}
