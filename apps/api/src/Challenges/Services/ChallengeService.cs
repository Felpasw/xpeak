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
        if (input.CategoryIds.Count == 0)
        {
            throw new NoCategoriesException();
        }

        var categoryIds = input.CategoryIds.Distinct().ToList();
        var existing = await db.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        var missing = categoryIds.FirstOrDefault(id => !existingSet.Contains(id));
        if (missing != default)
        {
            throw new CategoryNotFoundException(missing);
        }

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

        var categoryRows = categoryIds
            .Select(cid => new ChallengeCategory
            {
                Id = Guid.NewGuid(),
                ChallengeId = challenge.Id,
                CategoryId = cid,
            })
            .ToList();

        db.Challenges.Add(challenge);
        db.ChallengeMemberships.Add(ownerMembership);
        db.ChallengeCategories.AddRange(categoryRows);
        await db.SaveChangesAsync(ct);

        return new CreateChallengeResult(challenge, ownerMembership, categoryRows);
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

        if (rows.Count == 0)
        {
            return Array.Empty<ChallengeDetail>();
        }

        var challengeIds = rows.Select(r => r.Challenge.Id).ToList();
        var categoryMap = await db.ChallengeCategories
            .AsNoTracking()
            .Where(cc => challengeIds.Contains(cc.ChallengeId))
            .GroupBy(cc => cc.ChallengeId)
            .Select(g => new { ChallengeId = g.Key, Ids = g.Select(cc => cc.CategoryId).ToList() })
            .ToDictionaryAsync(x => x.ChallengeId, x => (IReadOnlyList<Guid>)x.Ids, ct);

        return rows
            .Select(r => new ChallengeDetail(
                r.Challenge,
                r.Membership,
                categoryMap.TryGetValue(r.Challenge.Id, out var ids) ? ids : Array.Empty<Guid>()))
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

        var categoryIds = await db.ChallengeCategories
            .AsNoTracking()
            .Where(cc => cc.ChallengeId == id)
            .Select(cc => cc.CategoryId)
            .ToListAsync(ct);

        return new ChallengeDetail(challenge, membership, categoryIds);
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
