using Microsoft.EntityFrameworkCore;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression.Services;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.CheckIns.Services;

public sealed class CheckInService(
    AppDbContext db,
    IProgressionService progression,
    IStreakService streak,
    TimeProvider time) : ICheckInService
{
    public async Task<CreateCheckInResult> CreateAsync(
        Guid userId,
        CreateCheckInInput input,
        CancellationToken ct = default)
    {
        var user = await db.Users.SingleAsync(u => u.Id == userId, ct);

        return input.WithMedia
            ? await PersistPendingAsync(user, input, ct)
            : await PersistPublishedAsync(user, input, ct);
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

        var user = await db.Users.SingleAsync(u => u.Id == checkIn.UserId, ct);
        return await ApplyScoringAndCommitAsync(user, checkIn, ct);
    }

    private async Task<CreateCheckInResult> PersistPublishedAsync(
        AppUser user,
        CreateCheckInInput input,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = input.Title.Trim(),
            PerformedAt = input.PerformedAt ?? now,
            DurationMinutes = input.DurationMinutes,
            Notes = input.Notes,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.CheckIns.Add(checkIn);

        return await ApplyScoringAndCommitAsync(user, checkIn, ct);
    }

    private async Task<CreateCheckInResult> PersistPendingAsync(
        AppUser user,
        CreateCheckInInput input,
        CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
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
        CancellationToken ct)
    {
        var xpEarned = progression.ComputeCheckInXp();
        var levelUp = progression.EvaluateLevelUp(user.Xp, user.Xp + xpEarned);

        checkIn.XpEarned = xpEarned;
        checkIn.ScoringSnapshot = new ScoringSnapshot(XpCalculator.CheckInXp, [], xpEarned);
        checkIn.UpdatedAt = time.GetUtcNow();

        user.Xp += xpEarned;
        user.Level = levelUp.NewLevel;

        await db.SaveChangesAsync(ct);

        var streakInfo = await ComputeStreakAsync(user, checkIn.PerformedAt, ct);
        return new CreateCheckInResult(checkIn, user, levelUp, streakInfo);
    }

    private async Task<StreakInfo> ComputeStreakAsync(
        AppUser user,
        DateTimeOffset performedAt,
        CancellationToken ct)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZone);
        var localAsOf = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(performedAt, tz).DateTime);
        return await streak.ComputeAsync(user.Id, localAsOf, ct);
    }
}
