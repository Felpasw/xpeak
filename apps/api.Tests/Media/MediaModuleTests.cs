using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Media;
using Xpeak.Api.Media.Providers.Cloudinary;
using Xpeak.Api.Media.Providers.Fake;

namespace Xpeak.Api.Tests.Media;

public sealed class MediaModuleTests
{
    [Fact]
    public void Fake_provider_resolves_to_FakeMediaStorage()
    {
        var services = new ServiceCollection();
        services.AddMedia(BuildConfig(("Media:Provider", "fake")));

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMediaStorage>().Should().BeOfType<FakeMediaStorage>();
    }

    [Fact]
    public void Cloudinary_provider_resolves_to_CloudinaryMediaStorage()
    {
        var services = new ServiceCollection();
        services.AddMedia(BuildConfig(
            ("Media:Provider", "cloudinary"),
            ("Cloudinary:CloudName", "test"),
            ("Cloudinary:ApiKey", "key"),
            ("Cloudinary:ApiSecret", "secret")));

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMediaStorage>().Should().BeOfType<CloudinaryMediaStorage>();
    }

    [Fact]
    public void Provider_selection_is_case_insensitive()
    {
        var services = new ServiceCollection();
        services.AddMedia(BuildConfig(("Media:Provider", "FaKe")));

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMediaStorage>().Should().BeOfType<FakeMediaStorage>();
    }

    [Fact]
    public void Default_provider_is_cloudinary_when_config_omits_it()
    {
        var services = new ServiceCollection();
        services.AddMedia(BuildConfig(
            ("Cloudinary:CloudName", "test"),
            ("Cloudinary:ApiKey", "key"),
            ("Cloudinary:ApiSecret", "secret")));

        using var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IMediaStorage>().Should().BeOfType<CloudinaryMediaStorage>();
    }

    [Fact]
    public void Unknown_provider_throws_at_startup()
    {
        var services = new ServiceCollection();

        FluentActions
            .Invoking(() => services.AddMedia(BuildConfig(("Media:Provider", "s3"))))
            .Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unknown media provider*");
    }

    private static IConfiguration BuildConfig(params (string Key, string Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();
}
