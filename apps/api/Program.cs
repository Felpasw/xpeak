using Microsoft.EntityFrameworkCore;
using Xpeak.Api.Endpoints;
using Xpeak.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Persistence
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// CORS for the mobile client (Capacitor) + Next.js dev server
const string CorsPolicy = "XpeakDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy => policy
        .WithOrigins("http://localhost:3001", "capacitor://localhost")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// OpenAPI (Swagger-like UI in dev)
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(CorsPolicy);

// Endpoints
app.MapHealthEndpoints();

app.Run();

// Marker type so integration tests can reference the app assembly.
public partial class Program;
