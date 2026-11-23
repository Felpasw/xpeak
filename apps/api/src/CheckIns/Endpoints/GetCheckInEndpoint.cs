using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Repositories;
using Xpeak.Api.Media;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.CheckIns.Endpoints;

/// <summary>GET /check_ins/{id} — detail payload for the mobile
/// Instagram-style publication route. Includes the author snapshot,
/// the reactions aggregate (counts + caller slice) and the live
/// comment count inline so the client renders the first paint
/// without secondary requests. Gated by
/// <see cref="ICheckInVisibility"/> — a hidden check-in surfaces as
/// 404, never 403.</summary>
public static class GetCheckInEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/check_ins/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags("Check-ins")
            .WithSummary("Returns the detail payload for a single check-in.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        ICheckInRepository repository,
        ICheckInReactionRepository reactionRepo,
        ICheckInCommentRepository commentRepo,
        ICheckInVisibility visibility,
        IMediaStorage storage,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!await visibility.CanSeeAsync(userId, id, ct))
        {
            return Results.NotFound();
        }

        var detail = await repository.GetDetailAsync(id, ct);
        if (detail is null)
        {
            return Results.NotFound();
        }

        var aggregate = await reactionRepo.GetAggregateAsync(id, userId, ct);
        var commentsCount = await commentRepo.CountVisibleAsync(id, ct);

        var preview = detail.FirstMedia is null
            ? null
            : new MediaPreview(
                detail.FirstMedia.Kind.ToString().ToLowerInvariant(),
                storage.BuildThumbUrl(detail.FirstMedia.StorageKey, detail.FirstMedia.Kind));

        var response = new CheckInDetailResponse(
            detail.CheckIn.Id,
            detail.CheckIn.Title,
            detail.CheckIn.XpEarned,
            detail.CheckIn.ScoringSnapshot,
            detail.CheckIn.PerformedAt,
            detail.CheckIn.DurationMinutes,
            detail.CheckIn.Notes,
            detail.HasMedia,
            preview,
            new CheckInAuthorDto(detail.Author.Id, detail.Author.Username, detail.Author.AvatarUrl),
            new CheckInReactionsSummary(aggregate.Counts, aggregate.Mine),
            commentsCount);

        return Results.Ok(response);
    }
}
