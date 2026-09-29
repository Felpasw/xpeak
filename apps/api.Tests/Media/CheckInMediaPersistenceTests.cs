using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media.Entities;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Media;

public sealed class CheckInMediaPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInMediaPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_media_row_with_dimensions_and_position()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var media = new CheckInMedia
        {
            Id = Guid.NewGuid(),
            CheckInId = fixture.CheckIn.Id,
            Kind = MediaKind.Photo,
            StorageKey = $"checkins/{fixture.CheckIn.Id}/{Guid.NewGuid()}.jpg",
            Width = 1920,
            Height = 1080,
            Position = 0,
        };
        db.CheckInMedia.Add(media);
        await db.SaveChangesAsync();

        var reloaded = await db.CheckInMedia
            .AsNoTracking()
            .SingleAsync(m => m.Id == media.Id);

        reloaded.CheckInId.Should().Be(fixture.CheckIn.Id);
        reloaded.Kind.Should().Be(MediaKind.Photo);
        reloaded.StorageKey.Should().Be(media.StorageKey);
        reloaded.Width.Should().Be(1920);
        reloaded.Height.Should().Be(1080);
        reloaded.DurationSeconds.Should().BeNull();
        reloaded.Position.Should().Be(0);
    }

    [Fact]
    public async Task Video_row_persists_duration_seconds()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var media = NewMedia(fixture, kind: MediaKind.Video, duration: 42);
        db.CheckInMedia.Add(media);
        await db.SaveChangesAsync();

        var row = await db.CheckInMedia.AsNoTracking().SingleAsync(m => m.Id == media.Id);
        row.Kind.Should().Be(MediaKind.Video);
        row.DurationSeconds.Should().Be(42);
    }

    [Fact]
    public async Task Rejects_media_referencing_a_missing_check_in()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orphan = new CheckInMedia
        {
            Id = Guid.NewGuid(),
            CheckInId = Guid.NewGuid(),
            Kind = MediaKind.Photo,
            StorageKey = $"orphan/{Guid.NewGuid()}.jpg",
            Position = 0,
        };
        db.CheckInMedia.Add(orphan);

        await FluentActions
            .Invoking(() => db.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Deleting_check_in_cascades_to_its_media()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.CheckInMedia.Add(NewMedia(fixture));
        db.CheckInMedia.Add(NewMedia(fixture));
        await db.SaveChangesAsync();

        var checkIn = await db.CheckIns.SingleAsync(c => c.Id == fixture.CheckIn.Id);
        db.CheckIns.Remove(checkIn);
        await db.SaveChangesAsync();

        var remaining = await db.CheckInMedia
            .AsNoTracking()
            .Where(m => m.CheckInId == fixture.CheckIn.Id)
            .CountAsync();
        remaining.Should().Be(0);
    }

    [Fact]
    public async Task Rejects_duplicate_storage_key()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var key = $"checkins/{fixture.CheckIn.Id}/duplicate.jpg";
        db.CheckInMedia.Add(NewMedia(fixture, storageKey: key));
        await db.SaveChangesAsync();

        db.CheckInMedia.Add(NewMedia(fixture, storageKey: key));

        await FluentActions
            .Invoking(() => db.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_blank_storage_key()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.CheckInMedia.Add(NewMedia(fixture, storageKey: "   "));

        await FluentActions
            .Invoking(() => db.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_non_positive_position()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var media = NewMedia(fixture);
        media.Position = -1;
        db.CheckInMedia.Add(media);

        await FluentActions
            .Invoking(() => db.SaveChangesAsync())
            .Should()
            .ThrowAsync<DbUpdateException>();
    }

    private async Task<Fixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"cim_{suffix}", Email = $"cim_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"grp-{Guid.NewGuid():N}", IsRoot = false };
        db.Groups.Add(group);

        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 15, WeightMultiplier = 1m };
        db.XpRules.Add(rule);

        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"cat-{Guid.NewGuid():N}",
            Name = "cat",
        };
        db.Categories.Add(category);

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = category.Id,
            GroupId = group.Id,
            XpEarned = 15,
            ScoringSnapshot = new ScoringSnapshot(15, [], 15),
            PerformedAt = DateTimeOffset.UtcNow,
        };
        db.CheckIns.Add(checkIn);

        await db.SaveChangesAsync();
        return new Fixture(user, group, category, checkIn);
    }

    private static CheckInMedia NewMedia(
        Fixture fixture,
        MediaKind kind = MediaKind.Photo,
        string? storageKey = null,
        int? duration = null) => new()
    {
        Id = Guid.NewGuid(),
        CheckInId = fixture.CheckIn.Id,
        Kind = kind,
        StorageKey = storageKey ?? $"checkins/{fixture.CheckIn.Id}/{Guid.NewGuid()}.jpg",
        Width = kind == MediaKind.Photo ? 1920 : null,
        Height = kind == MediaKind.Photo ? 1080 : null,
        DurationSeconds = duration,
        Position = 0,
    };

    private sealed record Fixture(AppUser User, Group Group, Category Category, CheckIn CheckIn);
}
