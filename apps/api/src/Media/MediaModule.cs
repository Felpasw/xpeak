using Xpeak.Api.Media.Providers.Cloudinary;
using Xpeak.Api.Media.Providers.Fake;

namespace Xpeak.Api.Media;

public static class MediaModule
{
    private const string CloudinaryProvider = "cloudinary";
    private const string FakeProvider = "fake";

    public static IServiceCollection AddMedia(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        var provider = configuration[$"{MediaOptions.SectionName}:Provider"] ?? CloudinaryProvider;
        return provider.ToLowerInvariant() switch
        {
            FakeProvider => AddFake(services),
            CloudinaryProvider => AddCloudinary(services, configuration),
            _ => throw new InvalidOperationException(
                $"Unknown media provider '{provider}'. Supported: '{CloudinaryProvider}', '{FakeProvider}'."),
        };
    }

    private static IServiceCollection AddFake(IServiceCollection services)
    {
        services.AddSingleton<IMediaStorage, FakeMediaStorage>();
        return services;
    }

    private static IServiceCollection AddCloudinary(
        IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.AddHttpClient<IMediaStorage, CloudinaryMediaStorage>();
        return services;
    }
}
