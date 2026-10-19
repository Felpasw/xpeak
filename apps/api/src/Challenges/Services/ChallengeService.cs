using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.Challenges.Entities;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.Challenges.Services;

public sealed class ChallengeService(AppDbContext db, TimeProvider time) : IChallengeService
{
    public async Task<CreateChallengeResult> CreateAsync(
        Guid ownerId,
        CreateChallengeInput input,
        CancellationToken ct = default)
    {
        var now = time.GetUtcNow();
        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Title = input.Title.Trim(),
            Description = input.Description,
            IconPublicId = input.IconPublicId,
            Visibility = input.Visibility,
            TargetLevel = input.TargetLevel,
            XpMultiplier = input.XpMultiplier,
            StartsAt = input.StartsAt,
            EndsAt = input.EndsAt,
            MemberCap = input.MemberCap,
            State = ChallengeState.Active,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var ownerMembership = new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = ownerId,
            Role = ChallengeMembershipRole.Owner,
            JoinedAt = now,
            Progress = MembershipProgress.Empty(input.TargetLevel),
        };

        var shareLink = new ChallengeInvitation
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            InvitedByUserId = ownerId,
            InviteeUserId = null,
            State = ChallengeInvitationState.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Challenges.Add(challenge);
        db.ChallengeMemberships.Add(ownerMembership);
        db.ChallengeInvitations.Add(shareLink);
        await db.SaveChangesAsync(ct);

        return new CreateChallengeResult(challenge, ownerMembership, shareLink);
    }

    public async Task<IReadOnlyList<ChallengeDetail>> ListForUserAsync(
        Guid userId,
        ChallengeState? stateFilter,
        CancellationToken ct = default)
    {
        var query = db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Join(db.Challenges.AsNoTracking(),
                m => m.ChallengeId,
                c => c.Id,
                (m, c) => new { Membership = m, Challenge = c });

        if (stateFilter.HasValue)
        {
            var state = stateFilter.Value;
            query = query.Where(x => x.Challenge.State == state);
        }

        var rows = await query
            .OrderByDescending(x => x.Challenge.CreatedAt)
            .ToListAsync(ct);

        return rows
            .Select(r => new ChallengeDetail(r.Challenge, r.Membership))
            .ToList();
    }

    public async Task<ChallengeDetail?> GetForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken ct = default)
    {
        var challenge = await db.Challenges
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, ct);
        if (challenge is null)
        {
            return null;
        }

        var membership = await db.ChallengeMemberships
            .AsNoTracking()
            .SingleOrDefaultAsync(m => m.ChallengeId == id && m.UserId == userId, ct);

        // Visibility: members always, non-members only on public.
        if (membership is null && challenge.Visibility != ChallengeVisibility.Public)
        {
            return null;
        }

        return new ChallengeDetail(challenge, membership);
    }

    public async Task<bool> DeleteAsync(
        Guid id,
        Guid callerId,
        CancellationToken ct = default)
    {
        var challenge = await db.Challenges
            .SingleOrDefaultAsync(c => c.Id == id, ct);
        if (challenge is null || challenge.OwnerId != callerId)
        {
            return false;
        }

        db.Challenges.Remove(challenge);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<JoinChallengeResult> JoinWithInvitationAsync(
        Guid invitationId,
        Guid callerId,
        CancellationToken ct = default)
    {
        var invitation = await db.ChallengeInvitations
            .AsNoTracking()
            .SingleOrDefaultAsync(i => i.Id == invitationId, ct)
            ?? throw new InvitationNotFoundException(invitationId);

        if (invitation.State != ChallengeInvitationState.Pending)
        {
            throw new InvitationNotActiveException(invitationId, invitation.State);
        }

        var challenge = await db.Challenges
            .AsNoTracking()
            .SingleAsync(c => c.Id == invitation.ChallengeId, ct);

        if (challenge.State != ChallengeState.Active)
        {
            throw new ChallengeNotActiveException(challenge.Id, challenge.State);
        }

        var alreadyMember = await db.ChallengeMemberships
            .AsNoTracking()
            .AnyAsync(m => m.ChallengeId == challenge.Id && m.UserId == callerId, ct);
        if (alreadyMember)
        {
            throw new AlreadyMemberException(challenge.Id, callerId);
        }

        if (challenge.MemberCap is int cap)
        {
            var count = await db.ChallengeMemberships
                .AsNoTracking()
                .CountAsync(m => m.ChallengeId == challenge.Id, ct);
            if (count >= cap)
            {
                throw new MemberCapReachedException(challenge.Id, cap);
            }
        }

        var now = time.GetUtcNow();
        var membership = new ChallengeMembership
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            UserId = callerId,
            Role = ChallengeMembershipRole.Member,
            JoinedAt = now,
            Progress = MembershipProgress.Empty(challenge.TargetLevel),
        };

        db.ChallengeMemberships.Add(membership);
        await db.SaveChangesAsync(ct);

        return new JoinChallengeResult(challenge, membership);
    }
}
