using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;
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
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var reloaded = await db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.Id == challenge.Id);

        reloaded.OwnerId.Should().Be(user.Id);
        reloaded.Visibility.Should().Be(ChallengeVisibility.Private);
        reloaded.State.Should().Be(ChallengeState.Active);
        reloaded.TargetLevel.Should().Be(10);
        reloaded.XpMultiplier.Should().Be(1.0m);
        reloaded.IconPublicId.Should().Be("challenges/sample-icon");
    }

    [Fact]
    public async Task Rejects_target_level_zero_or_negative()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        challenge.TargetLevel = 0;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_xp_multiplier_below_the_floor()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        challenge.XpMultiplier = 0.4m;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_xp_multiplier_above_the_ceiling()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        challenge.XpMultiplier = 1.6m;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_window_where_starts_at_is_not_before_ends_at()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        challenge.EndsAt = challenge.StartsAt;
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_blank_title()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        challenge.Title = "   ";
        db.Challenges.Add(challenge);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Enforces_unique_challenge_user_membership_pair()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        db.Challenges.Add(challenge);
        db.ChallengeMemberships.Add(new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = user.Id,
            Role = ChallengeMembershipRole.Owner,
            Progress = MembershipProgress.Empty(1500),
        });
        await db.SaveChangesAsync();

        db.ChallengeMemberships.Add(new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = user.Id,
            Role = ChallengeMembershipRole.Member,
            Progress = MembershipProgress.Empty(1500),
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Round_trips_membership_progress_jsonb()
    {
        var user = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var challenge = NewChallenge(user);
        db.Challenges.Add(challenge);
        var membership = new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = user.Id,
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

    private static Challenge NewChallenge(AppUser user) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = user.Id,
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

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ch_{suffix}", Email = $"ch_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
