using Xpeak.Api.Progression.Dto;
using Xpeak.Api.Progression.Services;

namespace Xpeak.Api.Progression.Endpoints;

/// <summary>
/// GET /groups/{groupId}/categories — active categories owned by
/// the group. Auth required; readable by any authenticated user so
/// the mobile onboarding / discovery flows can browse a group's
/// catalogue without a membership yet. Membership only kicks in on
/// write paths (check-in).
/// </summary>
public static class ListGroupCategoriesEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder app) =>
        app.MapGet("/groups/{groupId:guid}/categories", Handle)
            .RequireAuthorization()
            .WithTags("Progression")
            .WithSummary("Lists the active categories of a group.");

    private static async Task<IResult> Handle(
        Guid groupId,
        IProgressionService progression,
        CancellationToken ct)
    {
        var categories = await progression.ListActiveCategoriesAsync(groupId, ct);
        var response = new ListCategoriesResponse(
            categories.Select(CategoryResponse.From).ToList());
        return Results.Ok(response);
    }
}
