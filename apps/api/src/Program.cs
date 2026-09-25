using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Auth.Core;
using Xpeak.Api.Auth.Google;
using Xpeak.Api.Auth.Password;
using Xpeak.Api.CheckIns;
using Xpeak.Api.Endpoints;
using Xpeak.Api.Groups;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Progression;
using Xpeak.Api.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

builder.Services
    .AddUsers()
    .AddGroups()
    .AddProgression()
    .AddCheckIns()
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

app.Run();

// Marker type so integration tests can reference the app assembly.
public partial class Program;
