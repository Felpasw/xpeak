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

    private static CreateChallengeInput NewInput() => new(
        Title: "30 dias de treino",
        Description: "Levantar peso toda semana por 30 dias.",
        IconPublicId: "challenges/sample-icon",
        Visibility: ChallengeVisibility.Private,
        TargetLevel: 10,
        XpMultiplier: 1.0m,
        StartsAt: DateTimeOffset.UtcNow,
        EndsAt: DateTimeOffset.UtcNow.AddDays(30),
        MemberCap: null);

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
