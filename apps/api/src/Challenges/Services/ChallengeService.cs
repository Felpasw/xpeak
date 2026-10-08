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

        db.Challenges.Add(challenge);
        db.ChallengeMemberships.Add(ownerMembership);
        await db.SaveChangesAsync(ct);

        return new CreateChallengeResult(challenge, ownerMembership);
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
}
