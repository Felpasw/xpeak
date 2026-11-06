using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Auth.Google;
using Xpeak.Api.Auth.Password;
using Xpeak.Api.Challenges;
using Xpeak.Api.CheckIns;
using Xpeak.Api.Endpoints;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Media;
using Xpeak.Api.Progression;
using Xpeak.Api.Users;

var builder = WebApplication.CreateBuilder(args);

// Postgres data source with native enum mappings. Registered as
// a singleton (resolved lazily so tests can override the
// connection string before validation). Any time a new native
// enum lands, register it here AND call HasPostgresEnum<T> in
// AppDbContext — see Backgrounds for the shape.
builder.Services.AddSingleton<NpgsqlDataSource>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var dsBuilder = new NpgsqlDataSourceBuilder(config.GetConnectionString("Postgres"));
    dsBuilder.MapEnum<Backgrounds>("backgrounds");
    return dsBuilder.Build();
});

// Each WebApplicationFactory in integration tests spins up its own
// Postgres container and DbContext options; adding MapEnum pushes
// EF Core past its 20-service-provider warning threshold during a
// full suite run, so we silence it for this options signature.
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
    options
        .UseNpgsql(
            sp.GetRequiredService<NpgsqlDataSource>(),
            npgsql => npgsql.MapEnum<Backgrounds>("backgrounds"))
        .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning)));

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
app.UseCheckIns();
app.UseChallenges();
app.UseMedia();
app.UseUsers();

app.Run();

// Marker type so integration tests can reference the app assembly.
public partial class Program;
