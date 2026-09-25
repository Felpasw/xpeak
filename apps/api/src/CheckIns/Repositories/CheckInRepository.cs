using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;

namespace Xpeak.Api.CheckIns.Repositories;

public sealed class CheckInRepository(AppDbContext db) : ICheckInRepository
{
    public async Task<CheckInPage> ListAsync(
        Guid userId,
        int limit,
        string? cursor,
        Guid? groupId,
        CancellationToken ct = default)
    {
        var query = db.CheckIns
            .AsNoTracking()
            .Where(c => c.UserId == userId);

        if (groupId.HasValue)
        {
            var gid = groupId.Value;
            query = query.Where(c => c.GroupId == gid);
        }

        if (cursor is not null)
        {
            var (cursorAt, cursorId) = DecodeCursor(cursor);
            // Composite tuple comparison: order by (performed_at DESC, id DESC),
            // so the next page starts strictly before the cursor row.
            query = query.Where(c =>
                c.PerformedAt < cursorAt ||
                (c.PerformedAt == cursorAt && c.Id.CompareTo(cursorId) < 0));
        }

        // Take limit+1 to detect the presence of a next page without a
        // second round-trip.
        var rows = await query
            .OrderByDescending(c => c.PerformedAt)
            .ThenByDescending(c => c.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasNext = rows.Count > limit;
        if (hasNext)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var nextCursor = hasNext && rows.Count > 0
            ? EncodeCursor(rows[^1].PerformedAt, rows[^1].Id)
            : null;

        return new CheckInPage(rows, nextCursor);
    }

    private static string EncodeCursor(DateTimeOffset at, Guid id)
    {
        var raw = $"{at.ToString("O", CultureInfo.InvariantCulture)}|{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset At, Guid Id) DecodeCursor(string cursor)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|', 2);
        if (parts.Length != 2)
        {
            throw new ArgumentException("Malformed cursor.", nameof(cursor));
        }

        var at = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var id = Guid.Parse(parts[1]);
        return (at, id);
    }
}
