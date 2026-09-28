using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Repositories;

namespace Xpeak.Api.CheckIns.Endpoints;

/// <summary>GET /check_ins — cursor-paginated personal history.</summary>
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
        HttpContext ctx,
        int? limit,
        string? cursor,
        Guid? group_id,
        CancellationToken ct)
    {
        var sub = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (sub is null || !Guid.TryParse(sub, out var userId))
        {
            return Results.Unauthorized();
        }

        var effectiveLimit = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);
        var page = await repository.ListAsync(userId, effectiveLimit, cursor, group_id, ct);

        var response = new ListCheckInsResponse(
            page.Items.Select(CheckInResponse.From).ToList(),
            page.NextCursor);

        return Results.Ok(response);
    }
}
