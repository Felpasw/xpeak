using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ProgressCalculatorTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ProgressCalculatorTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Sums_only_whitelisted_in_window_for_the_membership_user()
    {
        var fixture = await SeedFixtureAsync();
        var challenge = await CreateChallengeAsync(fixture, targetLevel: 10,
            whitelistCategory: fixture.Whitelisted.Id);
        var otherUser = await CreateUserAsync();
        var now = DateTimeOffset.UtcNow;

        // Should count: owner, whitelisted category, in window.
        await InsertCheckInAsync(fixture.Owner.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 100, at: now.AddDays(1));
        await InsertCheckInAsync(fixture.Owner.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 50, at: now.AddDays(2));

        // Should NOT count: category not in whitelist.
        await InsertCheckInAsync(fixture.Owner.Id, fixture.OtherCategory.Id, fixture.Group.Id,
            xp: 500, at: now.AddDays(1));

        // Should NOT count: before challenge window (StartsAt - 1d).
        await InsertCheckInAsync(fixture.Owner.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 500, at: now.AddDays(-1));

        // Should NOT count: after challenge window (EndsAt + 1d).
        await InsertCheckInAsync(fixture.Owner.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 500, at: now.AddDays(31));

        // Should NOT count: other user, same whitelisted category + window.
        await InsertCheckInAsync(otherUser.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 500, at: now.AddDays(1));

        var ownerMembership = await LookupOwnerMembership(challenge.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var calc = scope.ServiceProvider.GetRequiredService<IProgressCalculator>();
        var progress = await calc.ComputeAsync(ownerMembership.Id);

        progress.Value.Should().Be(150);
        progress.Target.Should().Be(LevelCurve.XpForLevel(10));
        progress.AchievedAt.Should().BeNull();
    }

    [Fact]
    public async Task Flags_achieved_when_summed_xp_reaches_target_level()
    {
        var fixture = await SeedFixtureAsync();
        var challenge = await CreateChallengeAsync(fixture, targetLevel: 2,
            whitelistCategory: fixture.Whitelisted.Id);
        var now = DateTimeOffset.UtcNow;

        // target level 2 → XpForLevel(2) = 283. Give 400 to clear it.
        await InsertCheckInAsync(fixture.Owner.Id, fixture.Whitelisted.Id, fixture.Group.Id,
            xp: 400, at: now.AddDays(1));

        var ownerMembership = await LookupOwnerMembership(challenge.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var calc = scope.ServiceProvider.GetRequiredService<IProgressCalculator>();
        var progress = await calc.ComputeAsync(ownerMembership.Id);

        progress.Value.Should().Be(400);
        progress.Target.Should().Be(LevelCurve.XpForLevel(2));
        progress.AchievedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Zero_check_ins_produces_empty_progress()
    {
        var fixture = await SeedFixtureAsync();
        var challenge = await CreateChallengeAsync(fixture, targetLevel: 5,
            whitelistCategory: fixture.Whitelisted.Id);

        var ownerMembership = await LookupOwnerMembership(challenge.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var calc = scope.ServiceProvider.GetRequiredService<IProgressCalculator>();
        var progress = await calc.ComputeAsync(ownerMembership.Id);

        progress.Value.Should().Be(0);
        progress.Target.Should().Be(LevelCurve.XpForLevel(5));
        progress.AchievedAt.Should().BeNull();
    }

    private async Task<Challenge> CreateChallengeAsync(
        SeededFixture fixture,
        int targetLevel,
        Guid whitelistCategory)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(fixture.Owner.Id, new(
            Title: "P",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: targetLevel,
            XpMultiplier: 1.0m,
            StartsAt: now,
            EndsAt: now.AddDays(30),
            MemberCap: null,
            CategoryIds: new[] { whitelistCategory }));
        return result.Challenge;
    }

    private async Task<ChallengeMembership> LookupOwnerMembership(Guid challengeId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.ChallengeMemberships
            .AsNoTracking()
            .SingleAsync(m => m.ChallengeId == challengeId
                && m.Role == ChallengeMembershipRole.Owner);
    }

    private async Task InsertCheckInAsync(Guid userId, Guid categoryId, Guid groupId,
        int xp, DateTimeOffset at)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "t",
            UserId = userId,
            CategoryId = categoryId,
            GroupId = groupId,
            XpEarned = xp,
            ScoringSnapshot = new ScoringSnapshot(xp, [], xp),
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
    }

    private async Task<AppUser> CreateUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"pc_{suffix}", Email = $"pc_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var owner = new AppUser { UserName = $"pc_{suffix}", Email = $"pc_{suffix}@x.com" };
        (await users.CreateAsync(owner, "hunter22!")).Succeeded.Should().BeTrue();

        var group = new Group { Id = Guid.NewGuid(), Name = $"g_{suffix}", IsRoot = false };
        var config = new GroupConfig { GroupId = group.Id, StreakConfig = new StreakConfig(StreakMode.Daily) };
        var rule = new XpRule { Id = Guid.NewGuid(), BaseXp = 10, WeightMultiplier = 1.0m };
        var whitelisted = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"w_{suffix}",
            Name = "Whitelisted",
        };
        var other = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            XpRuleId = rule.Id,
            Slug = $"o_{suffix}",
            Name = "Other",
        };

        db.Groups.Add(group);
        db.GroupConfigs.Add(config);
        db.XpRules.Add(rule);
        db.Categories.AddRange(whitelisted, other);
        await db.SaveChangesAsync();

        return new SeededFixture(owner, group, whitelisted, other);
    }

    private sealed record SeededFixture(
        AppUser Owner,
        Group Group,
        Category Whitelisted,
        Category OtherCategory);
}
