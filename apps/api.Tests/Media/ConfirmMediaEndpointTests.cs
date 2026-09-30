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
using Xpeak.Api.Media;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Entities;
using Xpeak.Api.Media.Providers.Fake;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Media;

public sealed class ConfirmMediaEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ConfirmMediaEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/media",
            new { items = new[] { new { storageKey = "abc", kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Returns_404_when_check_in_does_not_exist()
    {
        var fixture = await SeedFixtureAsync();
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{Guid.NewGuid()}/media",
            new { items = new[] { new { storageKey = "abc", kind = "photo", position = 0 } } });

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
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = "abc", kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rejects_empty_items_list_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = Array.Empty<object>() });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_unknown_storage_key_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = "never-issued", kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_storage_key_issued_for_another_check_in_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var otherCheckIn = await SeedCheckInAsync(fixture);
        var targetCheckIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        // Ask for a presign against `otherCheckIn`, then try to confirm it
        // against `targetCheckIn`.
        var storageKey = await IssuePresignAsync(client, otherCheckIn.Id, "photo");

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{targetCheckIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_storage_key_whose_asset_is_missing_from_backend_with_422()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var storageKey = await IssuePresignAsync(client, checkIn.Id, "photo");
        // Note: no SeedMetadata — the storage backend still has no asset,
        // simulating a client that got a presign but never uploaded.

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Happy_path_persists_row_with_backend_verified_metadata()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var storageKey = await IssuePresignAsync(client, checkIn.Id, "photo");
        SeedBackendMetadata(storageKey, new MediaMetadata(1920, 1080, null));

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "photo", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ConfirmResponseDto>();
        body.Should().NotBeNull();
        body!.Media.Should().ContainSingle().Which.Should().BeEquivalentTo(new
        {
            CheckInId = checkIn.Id,
            Kind = "photo",
            StorageKey = storageKey,
            Width = (int?)1920,
            Height = (int?)1080,
            DurationSeconds = (int?)null,
            Position = 0,
        }, opts => opts.ExcludingMissingMembers());

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.CheckInMedia.AsNoTracking().SingleAsync(m => m.StorageKey == storageKey);
        row.Width.Should().Be(1920);
        row.Height.Should().Be(1080);
        row.Kind.Should().Be(MediaKind.Photo);
    }

    [Fact]
    public async Task Video_confirm_persists_duration_from_backend()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var storageKey = await IssuePresignAsync(client, checkIn.Id, "video");
        SeedBackendMetadata(storageKey, new MediaMetadata(1280, 720, DurationSeconds: 42));

        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "video", position = 0 } } });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ConfirmResponseDto>();
        body!.Media.Should().ContainSingle().Which.DurationSeconds.Should().Be(42);
    }

    [Fact]
    public async Task Consumed_storage_key_cannot_be_confirmed_twice()
    {
        var fixture = await SeedFixtureAsync();
        var checkIn = await SeedCheckInAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var storageKey = await IssuePresignAsync(client, checkIn.Id, "photo");
        SeedBackendMetadata(storageKey, new MediaMetadata(800, 600, null));

        var first = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "photo", position = 0 } } });
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var replay = await client.PostAsJsonAsync(
            $"/check_ins/{checkIn.Id}/media",
            new { items = new[] { new { storageKey = storageKey, kind = "photo", position = 0 } } });
        replay.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private async Task<string> IssuePresignAsync(HttpClient client, Guid checkInId, string kind)
    {
        var response = await client.PostAsJsonAsync(
            $"/check_ins/{checkInId}/media/presign",
            new { items = new[] { new { kind } } });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<PresignResponseDto>();
        return body!.Presigned[0].StorageKey;
    }

    private void SeedBackendMetadata(string storageKey, MediaMetadata metadata)
    {
        var storage = _factory.Services.GetRequiredService<IMediaStorage>();
        ((FakeMediaStorage)storage).SeedMetadata(storageKey, metadata);
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
        var user = new AppUser { UserName = $"cme_{suffix}", Email = $"cme_{suffix}@x.com" };
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
    private sealed record ConfirmResponseDto(IReadOnlyList<CheckInMediaDto> Media);
    private sealed record CheckInMediaDto(
        Guid Id,
        Guid CheckInId,
        string Kind,
        string StorageKey,
        int? Width,
        int? Height,
        int? DurationSeconds,
        int Position,
        DateTimeOffset CreatedAt);
}
