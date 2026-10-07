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
}
