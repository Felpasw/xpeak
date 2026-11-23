using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.Social.Dto;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social.Endpoints;

/// <summary>GET /check_ins/{id}/comments — cursor-paginated,
/// oldest-first. Soft-deleted rows are filtered out at the query
/// layer. The cursor is an opaque base64 string the caller is not
/// expected to interpret.</summary>
public static class ListCommentsEndpoint
{
    private const int DefaultLimit = 20;
    private const int MaxLimit = 50;

    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/check_ins/{id:guid}/comments", Handle)
            .RequireAuthorization()
            .WithTags("Social")
            .WithSummary("List the comments on a check-in.");

    private static async Task<IResult> Handle(
        Guid id,
        int? limit,
        string? cursor,
        ClaimsPrincipal principal,
        ICheckInVisibility visibility,
        ICheckInCommentRepository comments,
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

        var pageLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var page = await comments.ListPageAsync(id, cursor, pageLimit, ct);

        var items = page.Items
            .Select(r => new CommentResponse(
                r.Comment.Id,
                r.Comment.CheckInId,
                new CommentAuthorDto(r.Author.Id, r.Author.Username, r.Author.AvatarUrl),
                r.Comment.Body,
                r.Comment.CreatedAt))
            .ToList();

        return Results.Ok(new CommentsPageResponse(items, page.NextCursor));
    }
}
