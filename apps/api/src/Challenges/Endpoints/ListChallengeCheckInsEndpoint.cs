using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Challenges.Dto;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Users;

namespace Xpeak.Api.Challenges.Endpoints;

/// <summary>GET /challenges/{id}/check_ins — cursor-paginated
/// activity feed of every check-in a member of the challenge logged
/// inside the challenge window. 404 for non-members (no
/// existence-leak). Newest first, tiebroken by check-in id so the
/// cursor stays stable across identical timestamps.</summary>
public static class ListChallengeCheckInsEndpoint
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/challenges/{id:guid}/check_ins", Handle)
            .RequireAuthorization()
            .WithTags("Challenges")
            .WithSummary("Activity feed of the challenge members' check-ins.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        AppDbContext db,
        IMediaStorage storage,
        ICheckInReactionRepository reactionRepo,
        ICheckInCommentRepository commentRepo,
        int? limit,
        string? cursor,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var callerId))
        {
            return Results.Unauthorized();
        }

        var callerIsMember = await db.ChallengeMemberships
            .AsNoTracking()
            .AnyAsync(m => m.ChallengeId == id && m.UserId == callerId, ct);
        if (!callerIsMember)
        {
            return Results.NotFound();
        }

        var challenge = await db.Challenges
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new { c.StartsAt, c.EndsAt })
            .SingleOrDefaultAsync(ct);
        if (challenge is null)
        {
            return Results.NotFound();
        }

        var memberIds = await db.ChallengeMemberships
            .AsNoTracking()
            .Where(m => m.ChallengeId == id)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        var effectiveLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var query = db.CheckIns
            .AsNoTracking()
            .Where(c =>
                memberIds.Contains(c.UserId) &&
                c.PerformedAt >= challenge.StartsAt &&
                c.PerformedAt <= challenge.EndsAt);

        if (cursor is not null)
        {
            var (cursorAt, cursorId) = DecodeCursor(cursor);
            query = query.Where(c =>
                c.PerformedAt < cursorAt ||
                (c.PerformedAt == cursorAt && c.Id.CompareTo(cursorId) < 0));
        }

        var rows = await query
            .OrderByDescending(c => c.PerformedAt)
            .ThenByDescending(c => c.Id)
            .Take(effectiveLimit + 1)
            .ToListAsync(ct);

        var hasNext = rows.Count > effectiveLimit;
        if (hasNext)
        {
            rows.RemoveAt(rows.Count - 1);
        }

        var nextCursor = hasNext && rows.Count > 0
            ? EncodeCursor(rows[^1].PerformedAt, rows[^1].Id)
            : null;

        var authorIds = rows.Select(r => r.UserId).Distinct().ToList();
        var authors = await db.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, ct);

        var checkInIds = rows.Select(r => r.Id).ToList();
        var aggregates = await reactionRepo.GetAggregatesBatchAsync(checkInIds, callerId, ct);
        var commentCounts = await commentRepo.GetCountsBatchAsync(checkInIds, ct);

        var items = rows.Select(c =>
        {
            var media = db.CheckInMedia
                .AsNoTracking()
                .Where(m => m.CheckInId == c.Id)
                .OrderBy(m => m.Position)
                .Select(m => new FirstMediaSource(m.Kind, m.StorageKey))
                .FirstOrDefault();
            var hasMedia = media is not null;
            var aggregate = aggregates[c.Id];
            return new ChallengeCheckInFeedEntry(
                CheckInResponse.From(
                    c,
                    BuildPreview(storage, media),
                    hasMedia,
                    new CheckInReactionsSummary(aggregate.Counts, aggregate.Mine),
                    commentCounts[c.Id]),
                PublicUserResponse.From(authors[c.UserId]));
        }).ToList();

        return Results.Ok(new ListChallengeCheckInsResponse(items, nextCursor));
    }

    private static MediaPreview? BuildPreview(IMediaStorage storage, FirstMediaSource? source) =>
        source is null
            ? null
            : new MediaPreview(
                source.Kind.ToString().ToLowerInvariant(),
                storage.BuildThumbUrl(source.StorageKey, source.Kind));

    private static string EncodeCursor(DateTimeOffset at, Guid id)
    {
        var raw = $"{at.ToString("O", CultureInfo.InvariantCulture)}|{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTimeOffset At, Guid Id) DecodeCursor(string cursor)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('|', 2);
        var at = DateTimeOffset.Parse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        var id = Guid.Parse(parts[1]);
        return (at, id);
    }
}
