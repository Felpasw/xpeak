using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups;
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
    public async Task Sums_all_check_ins_in_window_for_the_membership_user()
    {
        var owner = await CreateUserAsync("pcowner");
        var categoryId = await EnsureCategoryAsync("pc-any");
        var challengeId = await CreateChallengeAsync(owner, targetLevel: 10);
        var otherUser = await CreateUserAsync("pcother");
        var now = DateTimeOffset.UtcNow;

        // Should count: owner, in window (category doesn't matter anymore).
        await InsertCheckInAsync(owner.Id, categoryId, xp: 100, at: now.AddDays(1));
        await InsertCheckInAsync(owner.Id, categoryId, xp: 50, at: now.AddDays(2));

        // Should NOT count: before challenge window (StartsAt - 1d).
        await InsertCheckInAsync(owner.Id, categoryId, xp: 500, at: now.AddDays(-1));

        // Should NOT count: after challenge window (EndsAt + 1d).
        await InsertCheckInAsync(owner.Id, categoryId, xp: 500, at: now.AddDays(31));

        // Should NOT count: other user, in window.
        await InsertCheckInAsync(otherUser.Id, categoryId, xp: 500, at: now.AddDays(1));

        var ownerMembership = await LookupOwnerMembership(challengeId);

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
        var owner = await CreateUserAsync("pcach");
        var categoryId = await EnsureCategoryAsync("pc-any-2");
        var challengeId = await CreateChallengeAsync(owner, targetLevel: 2);
        var now = DateTimeOffset.UtcNow;

        // target level 2 → XpForLevel(2) = 283. Give 400 to clear it.
        await InsertCheckInAsync(owner.Id, categoryId, xp: 400, at: now.AddDays(1));

        var ownerMembership = await LookupOwnerMembership(challengeId);

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
        var owner = await CreateUserAsync("pcempty");
        var challengeId = await CreateChallengeAsync(owner, targetLevel: 5);

        var ownerMembership = await LookupOwnerMembership(challengeId);

        await using var scope = _factory.Services.CreateAsyncScope();
        var calc = scope.ServiceProvider.GetRequiredService<IProgressCalculator>();
        var progress = await calc.ComputeAsync(ownerMembership.Id);

        progress.Value.Should().Be(0);
        progress.Target.Should().Be(LevelCurve.XpForLevel(5));
        progress.AchievedAt.Should().BeNull();
    }

    private async Task<Guid> CreateChallengeAsync(AppUser owner, int targetLevel)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        var now = DateTimeOffset.UtcNow;
        var result = await service.CreateAsync(owner.Id, new(
            Title: "P",
            Description: null,
            IconPublicId: null,
            Visibility: ChallengeVisibility.Private,
            TargetLevel: targetLevel,
            XpMultiplier: 1.0m,
            StartsAt: now,
            EndsAt: now.AddDays(30),
            MemberCap: null));
        return result.Challenge.Id;
    }

    private async Task<Guid> EnsureCategoryAsync(string slug)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        // SeedIds.DefaultXpRule is internal to the api assembly; the
        // test knows the stable value from the migration seed.
        var defaultXpRuleId = new Guid("00000000-0000-0000-0000-000000000010");
        var fullSlug = $"{slug}-{Guid.NewGuid():N}";
        var category = new Category
        {
            Id = Guid.NewGuid(),
            GroupId = GroupIds.Global,
            XpRuleId = defaultXpRuleId,
            Slug = fullSlug,
            Name = fullSlug,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category.Id;
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

    private async Task InsertCheckInAsync(Guid userId, Guid categoryId, int xp, DateTimeOffset at)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.CheckIns.Add(new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "t",
            UserId = userId,
            CategoryId = categoryId,
            GroupId = GroupIds.Global,
            XpEarned = xp,
            ScoringSnapshot = new ScoringSnapshot(xp, [], xp),
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
    }

    private async Task<AppUser> CreateUserAsync(string prefix)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"{prefix}_{suffix}", Email = $"{prefix}_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
