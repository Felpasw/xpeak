using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Providers.Fake;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Media;

public sealed class GetMediaUrlEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public GetMediaUrlEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            $"/check_ins/{Guid.NewGuid()}/media/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_check_in_does_not_exist()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.GetAsync(
            $"/check_ins/{Guid.NewGuid()}/media/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Returns_403_when_check_in_belongs_to_another_user()
    {
        var owner = await SeedFixtureAsync();
        var other = await SeedUserAsync();
        var checkIn = await SeedCheckInAsync(owner);
        var client = AuthenticatedClientFor(other);

        var response = await client.GetAsync(
            $"/check_ins/{checkIn.Id}/media/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Returns_404_when_media_id_does_not_belong_to_the_check_in()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.GetAsync(
            $"/check_ins/{checkIn.Id}/media/{Guid.NewGuid()}/url");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Happy_path_returns_signed_url_with_expected_ttl()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var mediaId = await SeedConfirmedMediaAsync(client, checkIn.Id);

        var response = await client.GetAsync(
            $"/check_ins/{checkIn.Id}/media/{mediaId}/url");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<MediaUrlResponse>();
        body.Should().NotBeNull();
        body!.Url.Should().StartWith("https://fake.local/get/");
        body.ExpiresIn.Should().Be(300);
    }

    private async Task<Guid> SeedConfirmedMediaAsync(HttpClient client, Guid checkInId)
    {
        var presign = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/media/presign",
            new { items = new[] { new { kind = "photo" } } });
        presign.EnsureSuccessStatusCode();
        var presignBody = await presign.Content.ReadFromJsonAsync<PresignResponseDto>();
        var storageKey = presignBody!.Presigned[0].StorageKey;

        var fake = (FakeMediaStorage)_factory.Services.GetRequiredService<IMediaStorage>();
        fake.SeedMetadata(storageKey, new MediaMetadata(1200, 800, null));

        var confirm = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/media",
            new { items = new[] { new { storageKey, kind = "photo", position = 0 } } });
        confirm.EnsureSuccessStatusCode();
        var confirmBody = await confirm.Content.ReadFromJsonAsync<ConfirmResponseDto>();
        return confirmBody!.Media[0].Id;
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
        var user = new AppUser { UserName = $"gmu_{suffix}", Email = $"gmu_{suffix}@x.com" };
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
    private sealed record ConfirmResponseDto(IReadOnlyList<ConfirmedMediaDto> Media);
    private sealed record ConfirmedMediaDto(Guid Id);
}
