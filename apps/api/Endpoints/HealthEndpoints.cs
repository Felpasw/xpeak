namespace Xpeak.Api.Endpoints;

public static class HealthEndpoints
{
    private static readonly Lazy<string> Version = new(() =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "VERSION")).Trim());

    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthResponse(
                Status: "ok",
                Version: Version.Value,
                Time: DateTimeOffset.UtcNow
            )))
            .WithName("Health")
            .WithSummary("Reports the app status, version and timestamp.");

        return app;
    }
}

public sealed record HealthResponse(string Status, string Version, DateTimeOffset Time);
