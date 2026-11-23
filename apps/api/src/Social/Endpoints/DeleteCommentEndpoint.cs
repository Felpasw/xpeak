using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Social.Repositories;

namespace Xpeak.Api.Social.Endpoints;

/// <summary>DELETE /comments/{id} — soft-delete. The caller must be
/// the author of the comment OR the owner of the parent check-in.
/// Third parties get 403; an already-deleted or missing comment
/// gets 404.</summary>
public static class DeleteCommentEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/comments/{id:guid}", Handle)
            .RequireAuthorization()
            .WithTags("Social")
            .WithSummary("Soft-delete a comment.");

    private static async Task<IResult> Handle(
        Guid id,
        ClaimsPrincipal principal,
        ICheckInCommentRepository comments,
        AppDbContext db,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var row = await comments.GetByIdAsync(id, ct);
        if (row is null || row.Comment.DeletedAt is not null)
        {
            return Results.NotFound();
        }

        var checkInOwnerId = await db.CheckIns
            .AsNoTracking()
            .Where(c => c.Id == row.Comment.CheckInId)
            .Select(c => c.UserId)
            .SingleAsync(ct);

        var isAuthor = row.Comment.UserId == userId;
        var isOwner = checkInOwnerId == userId;
        if (!isAuthor && !isOwner)
        {
            return Results.Forbid();
        }

        await comments.SoftDeleteAsync(id, ct);
        return Results.NoContent();
    }
}
