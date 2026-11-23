using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentValidation;
using Xpeak.Api.Social.Dto;
using Xpeak.Api.Social.Repositories;
using Xpeak.Api.Social.Services;

namespace Xpeak.Api.Social.Endpoints;

/// <summary>POST /check_ins/{id}/comments — author a new comment.
/// Body is trimmed by the validator (empty → 422, &gt;280 → 422)
/// and visibility is checked once via <see cref="ICheckInVisibility"/>.
/// </summary>
public static class CreateCommentEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapPost("/check_ins/{id:guid}/comments", Handle)
            .RequireAuthorization()
            .WithTags("Social")
            .WithSummary("Leave a comment on a check-in.");

    private static async Task<IResult> Handle(
        Guid id,
        CreateCommentRequest request,
        ClaimsPrincipal principal,
        IValidator<CreateCommentRequest> validator,
        ICheckInVisibility visibility,
        ICheckInCommentRepository comments,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return Results.UnprocessableEntity(new
            {
                errors = validation.Errors.Select(e => new { e.PropertyName, code = e.ErrorCode }),
            });
        }

        if (!await visibility.CanSeeAsync(userId, id, ct))
        {
            return Results.NotFound();
        }

        var row = await comments.AddAsync(id, userId, request.Body.Trim(), ct);

        var response = new CommentResponse(
            row.Comment.Id,
            row.Comment.CheckInId,
            new CommentAuthorDto(row.Author.Id, row.Author.Username, row.Author.AvatarUrl),
            row.Comment.Body,
            row.Comment.CreatedAt);

        return Results.Created($"/comments/{row.Comment.Id}", response);
    }
}
