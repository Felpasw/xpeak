using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Auth.Google;
using Xpeak.Api.Auth.Password;
using Xpeak.Api.Challenges;
using Xpeak.Api.CheckIns;
using Xpeak.Api.Endpoints;
using Xpeak.Api.Groups;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media;
using Xpeak.Api.Progression;
using Xpeak.Api.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// Enum fields on HTTP payloads serialize as snake_case strings
// ("private", "total_xp", "active") rather than the default int
// ordinal, matching the EF Core JSONB convention in AppDbContext.
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services
    .AddUsers()
    .AddGroups()
    .AddProgression()
    .AddCheckIns()
    .AddChallenges()
    .AddMedia(builder.Configuration)
    .AddAuthCore(builder.Configuration)
    .AddPasswordAuth()
    .AddGoogleAuth(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60";
        await ctx.HttpContext.Response.WriteAsync("rate_limited", ct);
    };
});

// CORS for the mobile client (Capacitor) + Next.js dev server.
const string CorsPolicy = "XpeakDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:3001", "capacitor://localhost")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()
        .WithExposedHeaders("Authorization"));
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.UseAuthCore();
app.UsePasswordAuth();
app.UseGoogleAuth();
app.UseProgression();
app.UseCheckIns();
app.UseChallenges();
app.UseMedia();

app.Run();

// Marker type so integration tests can reference the app assembly.
public partial class Program;
