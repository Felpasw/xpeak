using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Challenges;

public sealed class ChallengeInvitationPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ChallengeInvitationPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Round_trips_a_share_link_invitation()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var invitation = NewShareLinkInvitation(challenge, owner);
        db.ChallengeInvitations.Add(invitation);
        await db.SaveChangesAsync();

        var reloaded = await db.ChallengeInvitations
            .AsNoTracking()
            .SingleAsync(i => i.Id == invitation.Id);

        reloaded.ChallengeId.Should().Be(challenge.Id);
        reloaded.InvitedByUserId.Should().Be(owner.Id);
        reloaded.InviteeUserId.Should().BeNull();
        reloaded.State.Should().Be(ChallengeInvitationState.Pending);
    }

    [Fact]
    public async Task Round_trips_a_targeted_invitation()
    {
        var owner = await SeedUserAsync();
        var invitee = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        await db.SaveChangesAsync();

        var invitation = NewTargetedInvitation(challenge, owner, invitee);
        db.ChallengeInvitations.Add(invitation);
        await db.SaveChangesAsync();

        var reloaded = await db.ChallengeInvitations
            .AsNoTracking()
            .SingleAsync(i => i.Id == invitation.Id);

        reloaded.InviteeUserId.Should().Be(invitee.Id);
    }

    [Fact]
    public async Task Allows_multiple_share_link_invitations_on_the_same_challenge()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        db.ChallengeInvitations.Add(NewShareLinkInvitation(challenge, owner));
        db.ChallengeInvitations.Add(NewShareLinkInvitation(challenge, owner));

        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Cascades_delete_from_challenge_to_invitations()
    {
        var owner = await SeedUserAsync();
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var challenge = NewChallenge(owner);
        db.Challenges.Add(challenge);
        db.ChallengeInvitations.Add(NewShareLinkInvitation(challenge, owner));
        await db.SaveChangesAsync();

        db.Challenges.Remove(challenge);
        await db.SaveChangesAsync();

        var orphaned = await db.ChallengeInvitations
            .AsNoTracking()
            .CountAsync(i => i.ChallengeId == challenge.Id);

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
        TargetLevel = 10,
        XpMultiplier = 1.0m,
        StartsAt = DateTimeOffset.UtcNow,
        EndsAt = DateTimeOffset.UtcNow.AddDays(30),
        State = ChallengeState.Active,
    };

    private static ChallengeInvitation NewShareLinkInvitation(
        Challenge challenge, AppUser invitedBy) => new()
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            InvitedByUserId = invitedBy.Id,
            InviteeUserId = null,
            State = ChallengeInvitationState.Pending,
        };

    private static ChallengeInvitation NewTargetedInvitation(
        Challenge challenge, AppUser invitedBy, AppUser invitee) => new()
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            InvitedByUserId = invitedBy.Id,
            InviteeUserId = invitee.Id,
            State = ChallengeInvitationState.Pending,
        };

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"inv_{suffix}", Email = $"inv_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }
}
