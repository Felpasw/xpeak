using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengePersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengePersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_challenge_with_target_level()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var reloaded = await db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.Id == challenge.Id);

        reloaded.OwnerId.Should().Be(fixture.User.Id);
        reloaded.Visibility.Should().Be(ChallengeVisibility.Private);
        reloaded.State.Should().Be(ChallengeState.Active);
        reloaded.TargetLevel.Should().Be(10);
        reloaded.XpMultiplier.Should().Be(1.0m);
        reloaded.IconPublicId.Should().Be("challenges/sample-icon");
    }

    [Fact]
    public async Task Rejects_target_level_zero_or_negative()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        challenge.TargetLevel = 0;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_xp_multiplier_below_the_floor()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        challenge.XpMultiplier = 0.4m;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_xp_multiplier_above_the_ceiling()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        challenge.XpMultiplier = 1.6m;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_window_where_starts_at_is_not_before_ends_at()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        challenge.EndsAt = challenge.StartsAt;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_blank_title()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        challenge.Title = "   ";
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Enforces_unique_challenge_user_membership_pair()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        db.Challenges.Add(challenge);
        db.ChallengeMemberships.Add(new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = fixture.User.Id,
            Role = ChallengeMembershipRole.Owner,
            Progress = MembershipProgress.Empty(1500),
        });
        await db.SaveChangesAsync();

        db.ChallengeMemberships.Add(new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = fixture.User.Id,
            Role = ChallengeMembershipRole.Member,
            Progress = MembershipProgress.Empty(1500),
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Round_trips_membership_progress_jsonb()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(fixture);
        db.Challenges.Add(challenge);
        var membership = new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = fixture.User.Id,
            Role = ChallengeMembershipRole.Owner,
            Progress = new MembershipProgress(
                Value: 900,
                Target: 1500,
                AchievedAt: null),
        };
        db.ChallengeMemberships.Add(membership);
        await db.SaveChangesAsync();

        var reloaded = await db.ChallengeMemberships
            .AsNoTracking()
            .SingleAsync(m => m.Id == membership.Id);

        reloaded.Progress.Value.Should().Be(900);
        reloaded.Progress.Target.Should().Be(1500);
        reloaded.Progress.AchievedAt.Should().BeNull();
    }

    private static Challenge NewChallenge(SeededFixture fixture) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = fixture.User.Id,
        Title = "30 dias de treino",
        Description = "Levantar peso toda semana por 30 dias.",
        IconPublicId = "challenges/sample-icon",
        Visibility = ChallengeVisibility.Private,
        TargetLevel = 10,
        XpMultiplier = 1.0m,
        StartsAt = DateTimeOffset.UtcNow,
        EndsAt = DateTimeOffset.UtcNow.AddDays(30),
        State = ChallengeState.Active,
    };

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ch_{suffix}", Email = $"ch_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
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

    private sealed record SeededFixture(AppUser User, Group Group, Category Category);
}
