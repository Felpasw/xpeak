using FluentValidation;
using Xpeak.Api.Media.Endpoints;
using Xpeak.Api.Media.Providers.Cloudinary;
using Xpeak.Api.Media.Providers.Fake;
using Xpeak.Api.Media.Services;

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
        services.AddMemoryCache();
        services.AddSingleton<IPendingMediaCache, PendingMediaCache>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddValidatorsFromAssemblyContaining(typeof(MediaModule));

        var provider = configuration[$"{MediaOptions.SectionName}:Provider"] ?? CloudinaryProvider;
        return provider.ToLowerInvariant() switch
        {
            FakeProvider => AddFake(services),
            CloudinaryProvider => AddCloudinary(services, configuration),
            _ => throw new InvalidOperationException(
                $"Unknown media provider '{provider}'. Supported: '{CloudinaryProvider}', '{FakeProvider}'."),
        };
    }

    public static IEndpointRouteBuilder UseMedia(this IEndpointRouteBuilder app)
    {
        PresignMediaEndpoint.Map(app);
        ConfirmMediaEndpoint.Map(app);
        return app;
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
