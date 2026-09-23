using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Auth.Google;

namespace Xpeak.Api.Tests.Support;

/// <summary>Same as <see cref="XpeakWebApplicationFactory"/> but swaps the Google
/// temp-cookie handler for <see cref="TestGoogleCookieHandler"/>, so tests can
/// exercise <c>/auth/google/callback</c> without hitting Google.</summary>
public sealed class GoogleStubbedXpeakWebApplicationFactory : XpeakWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.AddTransient<TestGoogleCookieHandler>();
            services.PostConfigure<AuthenticationOptions>(options =>
            {
                var scheme = options.Schemes.FirstOrDefault(s => s.Name == GoogleAuthModule.TempCookieScheme);
                if (scheme is not null)
                {
                    scheme.HandlerType = typeof(TestGoogleCookieHandler);
                }
            });
        });
    }
}
