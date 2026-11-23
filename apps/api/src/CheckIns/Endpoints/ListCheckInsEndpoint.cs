using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Repositories;
using Xpeak.Api.Media;
using Xpeak.Api.Social.Repositories;

namespace Xpeak.Api.CheckIns.Endpoints;

/// <summary>GET /check_ins — cursor-paginated personal history,
/// with reactions aggregate and live comment count batched per
/// page so each row carries engagement badges without an N+1
/// against the social tables.</summary>
public static class ListCheckInsEndpoint
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 100;

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/check_ins", Handle)
            .RequireAuthorization()
            .WithTags("Check-ins")
            .WithSummary("Lists the caller's check-ins, newest first, cursor-paginated.");

    private static async Task<IResult> Handle(
        ClaimsPrincipal principal,
        ICheckInRepository repository,
        ICheckInReactionRepository reactionRepo,
        ICheckInCommentRepository commentRepo,
        IMediaStorage storage,
        HttpContext ctx,
        int? limit,
        string? cursor,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var effectiveLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var page = await repository.ListAsync(userId, effectiveLimit, cursor, ct);

        var ids = page.Items.Select(i => i.CheckIn.Id).ToList();
        var aggregates = await reactionRepo.GetAggregatesBatchAsync(ids, userId, ct);
        var counts = await commentRepo.GetCountsBatchAsync(ids, ct);

        var response = new ListCheckInsResponse(
            page.Items.Select(i =>
            {
                var aggregate = aggregates[i.CheckIn.Id];
                return CheckInResponse.From(
                    i.CheckIn,
                    BuildPreview(storage, i.FirstMedia),
                    i.HasMedia,
                    new CheckInReactionsSummary(aggregate.Counts, aggregate.Mine),
                    counts[i.CheckIn.Id]);
            }).ToList(),
            page.NextCursor);

        return Results.Ok(response);
    }

    private static MediaPreview? BuildPreview(IMediaStorage storage, FirstMediaSource? source) =>
        source is null
            ? null
            : new MediaPreview(
                source.Kind.ToString().ToLowerInvariant(),
                storage.BuildThumbUrl(source.StorageKey, source.Kind));
}
