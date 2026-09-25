using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CheckInPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_check_in_with_scoring_snapshot()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = fixture.User.Id,
            CategoryId = fixture.Category.Id,
            GroupId = fixture.Group.Id,
            XpEarned = 21,
            ScoringSnapshot = new ScoringSnapshot(
                15,
                [new ScoringMultiplier("category_weight", 1.40m)],
                21),
            PerformedAt = DateTimeOffset.UtcNow,
            DurationMinutes = 45,
            Notes = "leg day",
        };
        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync();

        var reloaded = await db.CheckIns
            .AsNoTracking()
            .SingleAsync(c => c.Id == checkIn.Id);

        reloaded.XpEarned.Should().Be(21);
        reloaded.GroupId.Should().Be(fixture.Group.Id);
        reloaded.Notes.Should().Be("leg day");
        reloaded.ScoringSnapshot.BaseXp.Should().Be(15);
        reloaded.ScoringSnapshot.Total.Should().Be(21);
        reloaded.ScoringSnapshot.Multipliers.Should().ContainSingle()
            .Which.Should().Be(new ScoringMultiplier("category_weight", 1.40m));
    }

    [Fact]
    public async Task Rejects_negative_xp_earned()
    {
        // 0 is the CLR default for int; EF Core substitutes it with the
        // column default when one exists. `xp_earned` has no default, so
        // 0 would be sent through and rejected — but using -1 makes the
        // intent unambiguous and matches the negative-base_xp precedent.
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(fixture);
        checkIn.XpEarned = -1;
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_zero_duration_minutes()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(fixture);
        checkIn.DurationMinutes = 0;
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_notes_over_280_characters()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(fixture);
        checkIn.Notes = new string('a', 281);
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_unknown_user_id_fk()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(fixture);
        checkIn.UserId = Guid.NewGuid();
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_unknown_category_id_fk()
    {
        var fixture = await SeedFixtureAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(fixture);
        checkIn.CategoryId = Guid.NewGuid();
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ci_{suffix}", Email = $"ci_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var groupConfig = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"s_{suffix}",
            Name = "Test",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(groupConfig);
        db.XpRules.Add(rule);
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return new SeededFixture(user, group, category);
    }

    private static CheckIn NewCheckIn(SeededFixture fixture) => new()
    {
        Id = Guid.NewGuid(),
        UserId = fixture.User.Id,
        CategoryId = fixture.Category.Id,
        GroupId = fixture.Group.Id,
        XpEarned = 10,
        ScoringSnapshot = new ScoringSnapshot(
            10,
            [new ScoringMultiplier("category_weight", 1.00m)],
            10),
        PerformedAt = DateTimeOffset.UtcNow,
    };
}
