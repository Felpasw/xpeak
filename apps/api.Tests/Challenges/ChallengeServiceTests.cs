using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Groups.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeServiceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengeServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_challenge_owner_membership_and_categories()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var result = await service.CreateAsync(fixture.User.Id, NewInput(fixture));

        result.Challenge.OwnerId.Should().Be(fixture.User.Id);
        result.Challenge.State.Should().Be(ChallengeState.Active);
        result.Challenge.TargetLevel.Should().Be(10);
        result.OwnerMembership.Role.Should().Be(ChallengeMembershipRole.Owner);
        result.OwnerMembership.UserId.Should().Be(fixture.User.Id);
        result.Categories.Should().ContainSingle()
            .Which.CategoryId.Should().Be(fixture.Category.Id);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.Id == result.Challenge.Id);
        reloaded.State.Should().Be(ChallengeState.Active);
        reloaded.TargetLevel.Should().Be(10);

        var memberships = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.ChallengeId == result.Challenge.Id)
            .ToListAsync();
        memberships.Should().ContainSingle();
        memberships[0].UserId.Should().Be(fixture.User.Id);
        memberships[0].Role.Should().Be(ChallengeMembershipRole.Owner);
        memberships[0].Progress.Target.Should().Be(10);
    }

    [Fact]
    public async Task Rejects_input_with_empty_category_list()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var input = NewInput(fixture) with { CategoryIds = Array.Empty<Guid>() };

        var act = async () => await service.CreateAsync(fixture.User.Id, input);

        await act.Should().ThrowAsync<NoCategoriesException>();
    }

    [Fact]
    public async Task Rejects_input_with_unknown_category_without_persisting_anything()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var unknownId = Guid.NewGuid();
        var input = NewInput(fixture) with { CategoryIds = new[] { unknownId } };

        var act = async () => await service.CreateAsync(fixture.User.Id, input);

        await act.Should().ThrowAsync<CategoryNotFoundException>();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.Challenges.CountAsync(c => c.OwnerId == fixture.User.Id)).Should().Be(0);
        (await db.ChallengeMemberships.CountAsync(m => m.UserId == fixture.User.Id)).Should().Be(0);
    }

    [Fact]
    public async Task Deduplicates_category_ids_in_the_input()
    {
        var fixture = await SeedFixtureAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var input = NewInput(fixture) with
        {
            CategoryIds = new[] { fixture.Category.Id, fixture.Category.Id },
        };

        var result = await service.CreateAsync(fixture.User.Id, input);

        result.Categories.Should().ContainSingle();
    }

    private static CreateChallengeInput NewInput(SeededFixture fixture) => new(
        Title: "30 dias de treino",
        Description: "Levantar peso toda semana por 30 dias.",
        IconPublicId: "challenges/sample-icon",
        Visibility: ChallengeVisibility.Private,
        TargetLevel: 10,
        XpMultiplier: 1.0m,
        StartsAt: DateTimeOffset.UtcNow,
        EndsAt: DateTimeOffset.UtcNow.AddDays(30),
        MemberCap: null,
        CategoryIds: new[] { fixture.Category.Id });

    private async Task<SeededFixture> SeedFixtureAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"cs_{suffix}", Email = $"cs_{suffix}@x.com" };
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
