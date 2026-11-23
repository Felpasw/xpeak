using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Social.Entities;

namespace Xpeak.Api.Social.Repositories;

public sealed class CheckInReactionRepository(AppDbContext db)
    : ICheckInReactionRepository
{
    public async Task<bool> ToggleAsync(
        Guid checkInId,
        Guid userId,
        string emoji,
        CancellationToken ct = default)
    {
        var existing = await db.CheckInReactions
            .SingleOrDefaultAsync(
                r => r.CheckInId == checkInId
                    && r.UserId == userId
                    && r.Emoji == emoji,
                ct);

        if (existing is not null)
        {
            db.CheckInReactions.Remove(existing);
            await db.SaveChangesAsync(ct);
            return false;
        }

        db.CheckInReactions.Add(new CheckInReaction
        {
            Id = Guid.NewGuid(),
            CheckInId = checkInId,
            UserId = userId,
            Emoji = emoji,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ReactionAggregate> GetAggregateAsync(
        Guid checkInId,
        Guid callerId,
        CancellationToken ct = default)
    {
        var rows = await db.CheckInReactions
            .AsNoTracking()
            .Where(r => r.CheckInId == checkInId)
            .Select(r => new { r.Emoji, r.UserId })
            .ToListAsync(ct);

        var counts = rows
            .GroupBy(r => r.Emoji)
            .ToDictionary(g => g.Key, g => g.Count());

        var mine = rows
            .Where(r => r.UserId == callerId)
            .Select(r => r.Emoji)
            .Distinct()
            .ToList();

        return new ReactionAggregate(counts, mine);
    }

    public async Task<IReadOnlyDictionary<Guid, ReactionAggregate>> GetAggregatesBatchAsync(
        IReadOnlyList<Guid> checkInIds,
        Guid callerId,
        CancellationToken ct = default)
    {
        if (checkInIds.Count == 0)
        {
            return new Dictionary<Guid, ReactionAggregate>();
        }

        var rows = await db.CheckInReactions
            .AsNoTracking()
            .Where(r => checkInIds.Contains(r.CheckInId))
            .Select(r => new { r.CheckInId, r.UserId, r.Emoji })
            .ToListAsync(ct);

        var byCheckIn = rows.GroupBy(r => r.CheckInId).ToDictionary(
            g => g.Key,
            g =>
            {
                var counts = g.GroupBy(r => r.Emoji)
                    .ToDictionary(gg => gg.Key, gg => gg.Count());
                var mine = g.Where(r => r.UserId == callerId)
                    .Select(r => r.Emoji)
                    .Distinct()
                    .ToList();
                return new ReactionAggregate(counts, mine);
            });

        var empty = new ReactionAggregate(
            new Dictionary<string, int>(),
            Array.Empty<string>());
        foreach (var id in checkInIds)
        {
            byCheckIn.TryAdd(id, empty);
        }

        return byCheckIn;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<ReactionUser>>> GetUsersAsync(
        Guid checkInId,
        int perEmojiCap,
        CancellationToken ct = default)
    {
        var rows = await db.CheckInReactions
            .AsNoTracking()
            .Where(r => r.CheckInId == checkInId)
            .OrderBy(r => r.CreatedAt)
            .Join(
                db.Users.AsNoTracking(),
                r => r.UserId,
                u => u.Id,
                (r, u) => new { r.Emoji, User = new ReactionUser(u.Id, u.UserName ?? "", u.AvatarUrl) })
            .ToListAsync(ct);

        return rows
            .GroupBy(r => r.Emoji)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ReactionUser>)g.Take(perEmojiCap).Select(x => x.User).ToList());
    }
}
