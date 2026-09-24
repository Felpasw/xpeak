using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Groups;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Auth.Google;

public sealed class GoogleOAuthEndpointTests : IClassFixture<GoogleStubbedXpeakWebApplicationFactory>
{
    private readonly GoogleStubbedXpeakWebApplicationFactory _factory;

    public GoogleOAuthEndpointTests(GoogleStubbedXpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
    });

    [Fact]
    public async Task New_user_path_creates_account_and_redirects_with_token()
    {
        var client = CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"newg_{suffix}@x.com";
        var googleUid = "g-" + Guid.NewGuid().ToString("N");

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        request.Headers.Add(TestGoogleCookieHandler.UidHeader, googleUid);
        request.Headers.Add(TestGoogleCookieHandler.EmailHeader, email);
        request.Headers.Add(TestGoogleCookieHandler.AvatarHeader, "https://x/avatar.png");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().StartWith("xpeak://auth/callback?token=");

        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var created = await users.Users.SingleOrDefaultAsync(u => u.GoogleUid == googleUid);
        created.Should().NotBeNull();
        created!.Email.Should().Be(email);
        created.AvatarUrl.Should().Be("https://x/avatar.png");
        created.UserName.Should().StartWith($"newg_{suffix}");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var joinedGlobal = await db.GroupMemberships
            .AsNoTracking()
            .AnyAsync(m => m.UserId == created.Id && m.GroupId == GroupIds.Global);
        joinedGlobal.Should().BeTrue();
    }

    [Fact]
    public async Task Existing_user_by_google_uid_is_reused_without_creating_a_new_account()
    {
        var client = CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var googleUid = "g-" + Guid.NewGuid().ToString("N");

        await using (var seed = _factory.Services.CreateAsyncScope())
        {
            var seedUsers = seed.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var existing = new AppUser
            {
                UserName = $"exist_{suffix}",
                Email = $"exist_{suffix}@x.com",
                EmailConfirmed = true,
                GoogleUid = googleUid,
            };
            (await seedUsers.CreateAsync(existing)).Succeeded.Should().BeTrue();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        request.Headers.Add(TestGoogleCookieHandler.UidHeader, googleUid);
        request.Headers.Add(TestGoogleCookieHandler.EmailHeader, $"changed_{suffix}@x.com");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().StartWith("xpeak://auth/callback?token=");

        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var matches = await users.Users.Where(u => u.GoogleUid == googleUid).ToListAsync();
        matches.Should().ContainSingle();
        matches[0].UserName.Should().Be($"exist_{suffix}");
    }

    [Fact]
    public async Task Existing_user_by_email_links_google_uid()
    {
        var client = CreateClient();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var email = $"link_{suffix}@x.com";
        var googleUid = "g-" + Guid.NewGuid().ToString("N");

        await using (var seed = _factory.Services.CreateAsyncScope())
        {
            var seedUsers = seed.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var existing = new AppUser
            {
                UserName = $"link_{suffix}",
                Email = email,
                EmailConfirmed = false,
            };
            (await seedUsers.CreateAsync(existing, "hunter22!")).Succeeded.Should().BeTrue();
        }

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        request.Headers.Add(TestGoogleCookieHandler.UidHeader, googleUid);
        request.Headers.Add(TestGoogleCookieHandler.EmailHeader, email);
        request.Headers.Add(TestGoogleCookieHandler.AvatarHeader, "https://x/pic.png");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var linked = await users.Users.SingleOrDefaultAsync(u => u.Email == email);
        linked.Should().NotBeNull();
        linked!.GoogleUid.Should().Be(googleUid);
        linked.AvatarUrl.Should().Be("https://x/pic.png");
    }

    [Fact]
    public async Task Failure_from_google_redirects_with_error()
    {
        var client = CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/auth/google/callback");
        request.Headers.Add(TestGoogleCookieHandler.ForceFailHeader, "1");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location!.ToString().Should().Be("xpeak://auth/callback?error=google_failed");
    }
}
