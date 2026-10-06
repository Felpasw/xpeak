using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Media;

public sealed class PresignMediaEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public PresignMediaEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/media/presign",
            new { items = new[] { new { kind = "photo" } } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_check_in_does_not_exist()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/media/presign",
            new { items = new[] { new { kind = "photo" } } });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_403_when_check_in_belongs_to_another_user()
    {
        var owner = await SeedFixtureAsync();
        var otherUser = await SeedUserAsync();
        var checkIn = await SeedCheckInAsync(owner);
        var client = AuthenticatedClientFor(otherUser);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items = new[] { new { kind = "photo" } } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rejects_empty_items_list_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_unknown_kind_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items = new[] { new { kind = "audio" } } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_more_than_ten_items_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var items = Enumerable.Range(0, 11).Select(_ => new { kind = "photo" }).ToArray();
        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Happy_path_returns_one_slot_per_item_with_unique_storage_keys()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items = new[] { new { kind = "photo" }, new { kind = "video" } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<PresignResponseDto>();
        body.Should().NotBeNull();
        body!.Presigned.Should().HaveCount(2);
        body.Presigned.Select(p => p.StorageKey).Distinct().Should().HaveCount(2);
        body.Presigned.Should().AllSatisfy(slot =>
        {
            slot.StorageKey.Should().StartWith($"checkins/{checkIn.Id:N}/");
            slot.Url.Should().NotBeNullOrWhiteSpace();
            slot.ExpiresIn.Should().Be(600);
        });
    }

    [Fact]
    public async Task Case_insensitive_kind_is_accepted()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media/presign",
            new { items = new[] { new { kind = "Photo" }, new { kind = "VIDEO" } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"pme_{suffix}", Email = $"pme_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await SeedUserAsync();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1m };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"s_{suffix}",
            Name = "T",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new SeededFixture(user, group, category);
    }

    private async Task<CheckIn> SeedCheckInAsync(SeededFixture fixture)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "test",
            UserId = fixture.User.Id,
            CategoryId = fixture.Category.Id,
            GroupId = fixture.Group.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = DateTimeOffset.UtcNow,
        };
        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync();
        return checkIn;
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private sealed record PresignResponseDto(IReadOnlyList<PresignSlotDto> Presigned);
    private sealed record PresignSlotDto(string StorageKey, string Url, int ExpiresIn);
}
