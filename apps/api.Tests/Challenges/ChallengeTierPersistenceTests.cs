using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeTierPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengeTierPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Round_trips_a_tier_row()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var tier = new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 30,
            Title = "Mago",
        };
        db.ChallengeTiers.Add(tier);
        await db.SaveChangesAsync();

        var reloaded = await db.ChallengeTiers
            .AsNoTracking()
            .SingleAsync(t => t.Id == tier.Id);

        reloaded.ChallengeId.Should().Be(challenge.Id);
        reloaded.MinLevel.Should().Be(30);
        reloaded.Title.Should().Be("Mago");
    }

    [Fact]
    public async Task Rejects_min_level_zero_or_negative()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var tier = new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 0,
            Title = "Mago",
        };
        db.ChallengeTiers.Add(tier);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_blank_title()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var tier = new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 10,
            Title = "   ",
        };
        db.ChallengeTiers.Add(tier);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Enforces_unique_min_level_per_challenge()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        db.ChallengeTiers.Add(new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 30,
            Title = "Mago",
        });
        await db.SaveChangesAsync();

        db.ChallengeTiers.Add(new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 30,
            Title = "Outro Mago",
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Allows_same_min_level_across_different_challenges()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var a = NewChallenge(owner);
        var b = NewChallenge(owner);
        db.Challenges.Add(a);
        db.Challenges.Add(b);
        db.ChallengeTiers.Add(new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = a.Id,
            MinLevel = 30,
            Title = "Mago",
        });
        db.ChallengeTiers.Add(new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = b.Id,
            MinLevel = 30,
            Title = "Mago",
        });

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Cascades_delete_from_challenge_to_tiers()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        db.ChallengeTiers.Add(new ChallengeTier
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            MinLevel = 10,
            Title = "Aprendiz",
        });
        await db.SaveChangesAsync();

        db.Challenges.Remove(challenge);
        await db.SaveChangesAsync();

        var orphaned = await db.ChallengeTiers
            .AsNoTracking()
            .CountAsync(t => t.ChallengeId == challenge.Id);

        orphaned.Should().Be(0);
    }

    private static Challenge NewChallenge(AppUser owner) => new()
    {
        Id = Guid.NewGuid(),
        OwnerId = owner.Id,
        Title = "30 dias de treino",
        Description = null,
        IconPublicId = null,
        Visibility = ChallengeVisibility.Private,
        TargetLevel = 60,
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
        var user = new AppUser { UserName = $"tr_{suffix}", Email = $"tr_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
