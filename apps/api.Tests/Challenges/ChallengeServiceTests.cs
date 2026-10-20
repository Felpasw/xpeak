using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Challenges.Services;
using Xpeak.Api.Infrastructure;
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
    public async Task Happy_path_persists_challenge_and_owner_membership()
    {
        var user = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var result = await service.CreateAsync(user.Id, NewInput());

        result.Challenge.OwnerId.Should().Be(user.Id);
        result.Challenge.State.Should().Be(ChallengeState.Active);
        result.Challenge.TargetLevel.Should().Be(10);
        result.OwnerMembership.Role.Should().Be(ChallengeMembershipRole.Owner);
        result.OwnerMembership.UserId.Should().Be(user.Id);
        result.OwnerMembership.Progress.Target.Should().Be(10);
        result.ShareLink.ChallengeId.Should().Be(result.Challenge.Id);
        result.ShareLink.InvitedByUserId.Should().Be(user.Id);
        result.ShareLink.InviteeUserId.Should().BeNull();
        result.ShareLink.State.Should().Be(ChallengeInvitationState.Pending);

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
        memberships[0].UserId.Should().Be(user.Id);
        memberships[0].Role.Should().Be(ChallengeMembershipRole.Owner);

        var invitations = await db.ChallengeInvitations
            .AsNoTracking()
            .Where(i => i.ChallengeId == result.Challenge.Id)
            .ToListAsync();
        invitations.Should().ContainSingle();
        invitations[0].Id.Should().Be(result.ShareLink.Id);
        invitations[0].InviteeUserId.Should().BeNull();
    }

    [Fact]
    public async Task Join_happy_path_inserts_a_member_membership()
    {
        var owner = await CreateUserAsync();
        var joiner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var join = await service.JoinWithInvitationAsync(created.ShareLink.Id, joiner.Id);

        join.Challenge.Id.Should().Be(created.Challenge.Id);
        join.Membership.UserId.Should().Be(joiner.Id);
        join.Membership.Role.Should().Be(ChallengeMembershipRole.Member);
        join.Membership.Progress.Target.Should().Be(created.Challenge.TargetLevel);

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var memberships = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.ChallengeId == created.Challenge.Id)
            .ToListAsync();
        memberships.Should().HaveCount(2);
        memberships.Should().Contain(m => m.UserId == joiner.Id && m.Role == ChallengeMembershipRole.Member);
    }

    [Fact]
    public async Task Join_rejects_an_unknown_invitation_id()
    {
        var joiner = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var act = async () => await service.JoinWithInvitationAsync(Guid.NewGuid(), joiner.Id);

        await act.Should().ThrowAsync<InvitationNotFoundException>();
    }

    [Fact]
    public async Task Join_rejects_an_invitation_whose_state_is_not_pending()
    {
        var owner = await CreateUserAsync();
        var joiner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using (var mutateScope = _factory.Services.CreateAsyncScope())
        {
            var db = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.ChallengeInvitations.SingleAsync(i => i.Id == created.ShareLink.Id);
            tracked.State = ChallengeInvitationState.Expired;
            await db.SaveChangesAsync();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var act = async () => await service.JoinWithInvitationAsync(created.ShareLink.Id, joiner.Id);

        var ex = await act.Should().ThrowAsync<InvitationNotActiveException>();
        ex.Which.State.Should().Be(ChallengeInvitationState.Expired);
    }

    [Fact]
    public async Task Join_rejects_a_challenge_that_is_not_active()
    {
        var owner = await CreateUserAsync();
        var joiner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using (var mutateScope = _factory.Services.CreateAsyncScope())
        {
            var db = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Challenges.SingleAsync(c => c.Id == created.Challenge.Id);
            tracked.State = ChallengeState.Completed;
            await db.SaveChangesAsync();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var act = async () => await service.JoinWithInvitationAsync(created.ShareLink.Id, joiner.Id);

        await act.Should().ThrowAsync<ChallengeNotActiveException>();
    }

    [Fact]
    public async Task Join_rejects_the_owner_who_is_already_a_member()
    {
        var owner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var act = async () => await service.JoinWithInvitationAsync(created.ShareLink.Id, owner.Id);

        await act.Should().ThrowAsync<AlreadyMemberException>();
    }

    [Fact]
    public async Task Join_rejects_once_member_cap_is_reached()
    {
        var owner = await CreateUserAsync();
        var joiner = await CreateUserAsync();
        var late = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner, memberCap: 2);
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        await service.JoinWithInvitationAsync(created.ShareLink.Id, joiner.Id);

        var act = async () => await service.JoinWithInvitationAsync(created.ShareLink.Id, late.Id);

        var ex = await act.Should().ThrowAsync<MemberCapReachedException>();
        ex.Which.Cap.Should().Be(2);
    }

    [Fact]
    public async Task End_happy_path_flips_state_and_deletes_invitations()
    {
        var owner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var ok = await service.EndAsync(created.Challenge.Id, owner.Id);

        ok.Should().BeTrue();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == created.Challenge.Id);
        reloaded.State.Should().Be(ChallengeState.Completed);

        var invitations = await db.ChallengeInvitations
            .AsNoTracking()
            .Where(i => i.ChallengeId == created.Challenge.Id)
            .CountAsync();
        invitations.Should().Be(0);
    }

    [Fact]
    public async Task End_returns_false_for_a_non_owner_caller()
    {
        var owner = await CreateUserAsync();
        var stranger = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var ok = await service.EndAsync(created.Challenge.Id, stranger.Id);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task End_returns_false_for_an_unknown_challenge()
    {
        var owner = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var ok = await service.EndAsync(Guid.NewGuid(), owner.Id);

        ok.Should().BeFalse();
    }

    [Fact]
    public async Task End_rejects_an_already_ended_challenge()
    {
        var owner = await CreateUserAsync();
        var created = await CreateChallengeAsync(owner);
        await using (var mutateScope = _factory.Services.CreateAsyncScope())
        {
            var db = mutateScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tracked = await db.Challenges.SingleAsync(c => c.Id == created.Challenge.Id);
            tracked.State = ChallengeState.Completed;
            await db.SaveChangesAsync();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var act = async () => await service.EndAsync(created.Challenge.Id, owner.Id);

        await act.Should().ThrowAsync<ChallengeAlreadyEndedException>();
    }

    [Fact]
    public async Task Create_persists_tiers_sorted_by_min_level()
    {
        var user = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var result = await service.CreateAsync(user.Id, NewInput() with
        {
            Tiers = new List<CreateChallengeTierInput>
            {
                new(MinLevel: 60, Title: "ArchMago"),
                new(MinLevel: 10, Title: " Aprendiz "),
                new(MinLevel: 30, Title: "Mago"),
            },
        });

        result.Tiers.Should().HaveCount(3);
        result.Tiers.Select(t => t.MinLevel).Should().ContainInOrder(10, 30, 60);
        result.Tiers.Select(t => t.Title).Should().ContainInOrder("Aprendiz", "Mago", "ArchMago");

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var reloaded = await db.ChallengeTiers
            .AsNoTracking()
            .Where(t => t.ChallengeId == result.Challenge.Id)
            .OrderBy(t => t.MinLevel)
            .ToListAsync();
        reloaded.Should().HaveCount(3);
        reloaded.Select(t => t.MinLevel).Should().ContainInOrder(10, 30, 60);
    }

    [Fact]
    public async Task Create_without_tiers_persists_an_empty_tier_list()
    {
        var user = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var result = await service.CreateAsync(user.Id, NewInput());

        result.Tiers.Should().BeEmpty();

        await using var verifyScope = _factory.Services.CreateAsyncScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var count = await db.ChallengeTiers
            .AsNoTracking()
            .CountAsync(t => t.ChallengeId == result.Challenge.Id);
        count.Should().Be(0);
    }

    [Fact]
    public async Task Create_with_eleven_tiers_throws_TooManyTiers()
    {
        var user = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var tiers = Enumerable.Range(1, 11)
            .Select(i => new CreateChallengeTierInput(MinLevel: i, Title: $"Tier {i}"))
            .ToList();

        var act = async () => await service.CreateAsync(user.Id, NewInput() with { Tiers = tiers });

        var ex = await act.Should().ThrowAsync<TooManyTiersException>();
        ex.Which.Count.Should().Be(11);
        ex.Which.Max.Should().Be(ChallengeService.MaxTiersPerChallenge);
    }

    [Fact]
    public async Task Create_with_duplicate_min_level_throws_DuplicateTier()
    {
        var user = await CreateUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();

        var tiers = new List<CreateChallengeTierInput>
        {
            new(MinLevel: 10, Title: "Aprendiz"),
            new(MinLevel: 10, Title: "Outro Aprendiz"),
        };

        var act = async () => await service.CreateAsync(user.Id, NewInput() with { Tiers = tiers });

        var ex = await act.Should().ThrowAsync<DuplicateTierException>();
        ex.Which.MinLevel.Should().Be(10);
    }

    private async Task<CreateChallengeResult> CreateChallengeAsync(AppUser owner, int? memberCap = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IChallengeService>();
        return await service.CreateAsync(owner.Id, NewInput(memberCap));
    }

    private static CreateChallengeInput NewInput(int? memberCap = null) => new(
        Title: "30 dias de treino",
        Description: "Levantar peso toda semana por 30 dias.",
        IconPublicId: "challenges/sample-icon",
        Visibility: ChallengeVisibility.Private,
        TargetLevel: 10,
        XpMultiplier: 1.0m,
        StartsAt: DateTimeOffset.UtcNow,
        EndsAt: DateTimeOffset.UtcNow.AddDays(30),
        MemberCap: memberCap);

    private async Task<AppUser> CreateUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"cs_{suffix}", Email = $"cs_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
