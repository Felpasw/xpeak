using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CreateCheckInEndpointTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CreateCheckInEndpointTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Rejects_without_a_token_with_401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = Guid.NewGuid(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Happy_path_returns_201_with_check_in_user_and_streak()
    {
        var fixture = await SeedFixtureAsync(baseXp: 15, weight: 1.40m);
        await JoinGroupAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = fixture.Category.Id,
            durationMinutes = 45,
            notes = "leg day",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<CreateCheckInResponseDto>();
        body.Should().NotBeNull();

        body!.CheckIn.CategoryId.Should().Be(fixture.Category.Id);
        body.CheckIn.GroupId.Should().Be(fixture.Group.Id);
        body.CheckIn.XpEarned.Should().Be(21);
        body.CheckIn.Notes.Should().Be("leg day");
        body.CheckIn.DurationMinutes.Should().Be(45);
        body.CheckIn.ScoringSnapshot.BaseXp.Should().Be(15);
        body.CheckIn.ScoringSnapshot.Total.Should().Be(21);

        body.User.Xp.Should().Be(21);
        body.User.Level.Should().Be(0);
        body.User.LeveledUp.Should().BeFalse();

        body.Streak.Current.Should().Be(1);
        body.Streak.Longest.Should().Be(1);
        body.Streak.Unit.Should().Be("day");
    }

    [Fact]
    public async Task Rejects_with_403_when_user_is_not_a_member_of_the_group()
    {
        var fixture = await SeedFixtureAsync();
        // Skip JoinGroupAsync — user is not a member.
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = fixture.Category.Id,
        });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rejects_with_404_when_category_id_is_unknown()
    {
        var fixture = await SeedFixtureAsync();
        await JoinGroupAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = Guid.NewGuid(),
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Rejects_with_422_when_notes_exceed_280_characters()
    {
        var fixture = await SeedFixtureAsync();
        await JoinGroupAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = fixture.Category.Id,
            notes = new string('a', 281),
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Rejects_with_422_when_duration_minutes_is_not_positive()
    {
        var fixture = await SeedFixtureAsync();
        await JoinGroupAsync(fixture);
        var client = AuthenticatedClientFor(fixture.User);

        var response = await client.PostAsJsonAsync("/check_ins", new
        {
            categoryId = fixture.Category.Id,
            durationMinutes = 0,
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);

    private sealed record CreateCheckInResponseDto(
        CheckInBody CheckIn,
        UserBody User,
        StreakBody Streak);

    private sealed record CheckInBody(
        Guid Id,
        Guid CategoryId,
        Guid GroupId,
        int XpEarned,
        ScoringSnapshotBody ScoringSnapshot,
        DateTimeOffset PerformedAt,
        int? DurationMinutes,
        string? Notes);

    private sealed record ScoringSnapshotBody(
        int BaseXp,
        IReadOnlyList<MultiplierBody> Multipliers,
        int Total);

    private sealed record MultiplierBody(string Source, decimal Value);

    private sealed record UserBody(
        Guid Id,
        int Xp,
        int Level,
        bool LeveledUp,
        int LevelsGained);

    private sealed record StreakBody(int Current, int Longest, string Unit);

    private HttpClient AuthenticatedClientFor(AppUser user)
    {
        using var scope = _factory.Services.CreateScope();
        var issuer = scope.ServiceProvider.GetRequiredService<JwtTokenIssuer>();
        var token = issuer.Issue(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Value);
        return client;
    }

    private async Task<SeededFixture> SeedFixtureAsync(int baseXp = 10, decimal weight = 1.0m)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ep_{suffix}", Email = $"ep_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = baseXp, WeightMultiplier = weight };
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

    private async Task JoinGroupAsync(SeededFixture fixture)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.GroupMemberships.Add(new GroupMembership
        {
            UserId = fixture.User.Id,
            GroupId = fixture.Group.Id,
            JoinedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }
}
