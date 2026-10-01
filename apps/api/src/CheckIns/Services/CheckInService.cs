using Microsoft.EntityFrameworkCore;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Groups.Services;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Progression.Services;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

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

        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);

        return input.WithMedia
            ? await PersistPendingAsync(user, category, input, ct)
            : await PersistPublishedAsync(user, category, input, ct);
    }

    public async Task<CreateCheckInResult> PublishAsync(
        Guid checkInId,
        CancellationToken ct = default)
    {
        var checkIn = await db.CheckIns.SingleAsync(c => c.Id == checkInId, ct);
        if (checkIn.XpEarned != 0)
        {
            throw new CheckInAlreadyPublishedException(checkInId);
        }

        var category = await progression.GetCategoryByIdAsync(checkIn.CategoryId, ct)
            ?? throw new CategoryNotFoundException(checkIn.CategoryId);
        var user = await db.Users.SingleAsync(u => u.Id == checkIn.UserId, ct);

        return await ApplyScoringAndCommitAsync(user, checkIn, category, ct);
    }

    private async Task<CreateCheckInResult> PersistPublishedAsync(
        AppUser user,
        Category category,
        CreateCheckInInput input,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = category.Id,
            GroupId = category.GroupId,      // server-populated, never from client
            Title = input.Title.Trim(),
            PerformedAt = input.PerformedAt ?? now,
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CheckIns.Add(checkIn);

        return await ApplyScoringAndCommitAsync(user, checkIn, category, ct);
    }

    private async Task<CreateCheckInResult> PersistPendingAsync(
        AppUser user,
        Category category,
        CreateCheckInInput input,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            CategoryId = category.Id,
            GroupId = category.GroupId,
            Title = input.Title.Trim(),
            XpEarned = 0,
            ScoringSnapshot = new ScoringSnapshot(0, [], 0),
            PerformedAt = input.PerformedAt ?? now,
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync(ct);

        var noLevelUp = new LevelUpResult(false, user.Level, 0);
        var noStreak = new StreakInfo(0, 0, StreakUnit.Day);
        return new CreateCheckInResult(checkIn, user, noLevelUp, noStreak);
    }

    private async Task<CreateCheckInResult> ApplyScoringAndCommitAsync(
        AppUser user,
        CheckIn checkIn,
        Category category,
        CancellationToken ct)
    {
        var scoring = await BuildScoringAsync(category, ct);
        var levelUp = progression.EvaluateLevelUp(user.Xp, user.Xp + scoring.XpEarned);

        checkIn.XpEarned = scoring.XpEarned;
        checkIn.ScoringSnapshot = scoring.Snapshot;
        checkIn.UpdatedAt = time.GetUtcNow();

        user.Xp += scoring.XpEarned;
        user.Level = levelUp.NewLevel;

        await db.SaveChangesAsync(ct);

        var streakInfo = await ComputeStreakAsync(user, category.GroupId, checkIn.PerformedAt, ct);
        return new CreateCheckInResult(checkIn, user, levelUp, streakInfo);
    }

    private async Task<ScoringResult> BuildScoringAsync(Category category, CancellationToken ct)
    {
        var rule = await progression.GetRuleAsync(category.XpRuleId, ct)
            ?? throw new InvalidOperationException(
                $"Category '{category.Id}' references missing rule '{category.XpRuleId}'.");

        var xpEarned = progression.ComputeXp(rule);
        var snapshot = new ScoringSnapshot(
            rule.BaseXp,
            [new ScoringMultiplier("category_weight", rule.WeightMultiplier)],
            xpEarned);
        return new ScoringResult(xpEarned, snapshot);
    }

    private async Task<StreakInfo> ComputeStreakAsync(
        AppUser user,
        Guid groupId,
        DateTimeOffset performedAt,
        CancellationToken ct)
    {
        // `asOf` must be the check-in's date in the USER's timezone —
        // otherwise a 22:00 SP check-in would report streak "as of
        // tomorrow UTC" which never matches yesterday's local run.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
        var localAsOf = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(performedAt, tz).DateTime);
        return await streak.ComputeAsync(user.Id, groupId, localAsOf, ct);
    }

    private readonly record struct ScoringResult(int XpEarned, ScoringSnapshot Snapshot);
}
