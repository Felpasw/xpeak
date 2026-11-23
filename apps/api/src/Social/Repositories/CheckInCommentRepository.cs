using System.Text;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Social.Entities;

namespace Xpeak.Api.Social.Repositories;

public sealed class CheckInCommentRepository(AppDbContext db)
    : ICheckInCommentRepository
{
    public async Task<CommentRow> AddAsync(
        Guid checkInId,
        Guid userId,
        string body,
        CancellationToken ct = default)
    {
        var comment = new CheckInComment
        {
            Id = Guid.NewGuid(),
            CheckInId = checkInId,
            UserId = userId,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        db.CheckInComments.Add(comment);
        await db.SaveChangesAsync(ct);

        return (await GetByIdAsync(comment.Id, ct))!;
    }

    public async Task<CommentRow?> GetByIdAsync(
        Guid commentId,
        CancellationToken ct = default) =>
        await db.CheckInComments
            .AsNoTracking()
            .Where(c => c.Id == commentId)
            .Join(
                db.Users.AsNoTracking(),
                c => c.UserId,
                u => u.Id,
                (c, u) => new CommentRow(
                    c,
                    new CommentAuthor(u.Id, u.UserName ?? "", u.AvatarUrl)))
            .SingleOrDefaultAsync(ct);

    public async Task<CheckInComment?> SoftDeleteAsync(
        Guid commentId,
        CancellationToken ct = default)
    {
        var comment = await db.CheckInComments
            .SingleOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null || comment.DeletedAt is not null)
        {
            return null;
        }

        comment.DeletedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task<CommentPage> ListPageAsync(
        Guid checkInId,
        string? cursor,
        int limit,
        CancellationToken ct = default)
    {
        var query = db.CheckInComments
            .AsNoTracking()
            .Where(c => c.CheckInId == checkInId && c.DeletedAt == null);

        if (TryDecodeCursor(cursor, out var createdAt, out var afterId))
        {
            query = query.Where(c =>
                c.CreatedAt > createdAt
                || (c.CreatedAt == createdAt && c.Id.CompareTo(afterId) > 0));
        }

        var rows = await query
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Take(limit + 1)
            .Join(
                db.Users.AsNoTracking(),
                c => c.UserId,
                u => u.Id,
                (c, u) => new CommentRow(
                    c,
                    new CommentAuthor(u.Id, u.UserName ?? "", u.AvatarUrl)))
            .ToListAsync(ct);

        string? next = null;
        if (rows.Count > limit)
        {
            var last = rows[limit - 1];
            next = EncodeCursor(last.Comment.CreatedAt, last.Comment.Id);
            rows = rows.Take(limit).ToList();
        }

        return new CommentPage(rows, next);
    }

    public async Task<int> CountVisibleAsync(
        Guid checkInId,
        CancellationToken ct = default) =>
        await db.CheckInComments
            .AsNoTracking()
            .CountAsync(c => c.CheckInId == checkInId && c.DeletedAt == null, ct);

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsBatchAsync(
        IReadOnlyList<Guid> checkInIds,
        CancellationToken ct = default)
    {
        if (checkInIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        var rows = await db.CheckInComments
            .AsNoTracking()
            .Where(c => checkInIds.Contains(c.CheckInId) && c.DeletedAt == null)
            .GroupBy(c => c.CheckInId)
            .Select(g => new { CheckInId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var byCheckIn = rows.ToDictionary(r => r.CheckInId, r => r.Count);
        foreach (var id in checkInIds)
        {
            byCheckIn.TryAdd(id, 0);
        }

        return byCheckIn;
    }

    private static string EncodeCursor(DateTimeOffset createdAt, Guid id)
    {
        var raw = $"{createdAt.UtcTicks}|{id:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static bool TryDecodeCursor(
        string? cursor,
        out DateTimeOffset createdAt,
        out Guid id)
    {
        createdAt = default;
        id = default;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var parts = raw.Split('|', 2);
            if (parts.Length != 2) return false;
            if (!long.TryParse(parts[0], out var ticks)) return false;
            if (!Guid.TryParseExact(parts[1], "N", out var parsed)) return false;
            createdAt = new DateTimeOffset(ticks, TimeSpan.Zero);
            id = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
