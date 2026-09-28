using Microsoft.EntityFrameworkCore;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.CheckIns.Services;

public sealed class CheckInService(
    AppDbContext db,
    IProgressionService progression,
    IGroupService groups,
    IStreakService streak,
    TimeProvider time) : ICheckInService
{
    public async Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default)
    {
        var category = await progression.GetCategoryByIdAsync(input.CategoryId, ct)
            ?? throw new CategoryNotFoundException(input.CategoryId);

        var isMember = await groups.IsMemberAsync(userId, category.GroupId, ct);
        if (!isMember)
        {
            throw new NotAGroupMemberException(userId, category.GroupId);
        }

        var rule = await progression.GetRuleAsync(category.XpRuleId, ct)
            ?? throw new InvalidOperationException(
                $"Category '{category.Id}' references missing rule '{category.XpRuleId}'.");

        var xpEarned = progression.ComputeXp(rule);
        var snapshot = new ScoringSnapshot(
            rule.BaseXp,
            [new ScoringMultiplier("category_weight", rule.WeightMultiplier)],
            xpEarned);

        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);
        var prevXp = user.Xp;
        var newXp = prevXp + xpEarned;
        var levelUp = progression.EvaluateLevelUp(prevXp, newXp);

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CategoryId = category.Id,
            GroupId = category.GroupId,      // server-populated, never from client
            XpEarned = xpEarned,
            ScoringSnapshot = snapshot,
            PerformedAt = time.GetUtcNow(),
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
        };
        db.CheckIns.Add(checkIn);

        user.Xp = newXp;
        user.Level = levelUp.NewLevel;

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        // Post-commit: derive streak for the response payload.
        // `asOf` must be the check-in's date in the USER's timezone —
        // otherwise a 22:00 SP check-in would report streak "as of
        // tomorrow UTC" which never matches yesterday's local run.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
        var localAsOf = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(checkIn.PerformedAt, tz).DateTime);
        var streakInfo = await streak.ComputeAsync(
            userId,
            category.GroupId,
            localAsOf,
            ct);

        return new CreateCheckInResult(checkIn, user, levelUp, streakInfo);
    }
}
