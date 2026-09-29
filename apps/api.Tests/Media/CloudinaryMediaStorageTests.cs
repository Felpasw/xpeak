using System.Net;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Providers.Cloudinary;

namespace Xpeak.Api.Tests.Media;

public sealed class CloudinaryMediaStorageTests : IDisposable
{
    // Fixed credentials + clock so every signature is byte-for-byte reproducible.
    private const string Cloud = "test-cloud";
    private const string ApiKey = "111222333444555";
    private const string ApiSecret = "abcdefghijklmnop";
    private static readonly DateTimeOffset FixedNow = new(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly PresignOptions PhotoTenMin =
        new("image/jpeg", 1_024, TimeSpan.FromMinutes(10));

    private readonly WireMockServer _cloudinary = WireMockServer.Start();

    public void Dispose() => _cloudinary.Stop();

    private CloudinaryMediaStorage NewStorage()
    {
        var options = Options.Create(new CloudinaryOptions
        {
            CloudName = Cloud,
            ApiKey = ApiKey,
            ApiSecret = ApiSecret,
            ApiBaseUrl = _cloudinary.Urls[0],
        });
        var http = new HttpClient { BaseAddress = new Uri(_cloudinary.Urls[0]) };
        var time = new FakeTimeProvider(FixedNow);
        return new CloudinaryMediaStorage(options, http, time);
    }

    [Fact]
    public async Task PresignedPutUrl_targets_the_cloud_upload_endpoint_for_the_right_resource_kind()
    {
        var storage = NewStorage();

        var photo = await storage.PresignedPutUrlAsync(
            "checkins/abc/photo.jpg", PhotoTenMin, CancellationToken.None);
        var video = await storage.PresignedPutUrlAsync(
            "checkins/abc/clip.mp4",
            PhotoTenMin with { ContentType = "video/mp4" },
            CancellationToken.None);

        photo.Url.Should().StartWith($"{_cloudinary.Urls[0]}/v1_1/{Cloud}/image/upload");
        video.Url.Should().StartWith($"{_cloudinary.Urls[0]}/v1_1/{Cloud}/video/upload");
    }

    [Fact]
    public async Task PresignedPutUrl_embeds_api_key_timestamp_signature_and_public_id()
    {
        var storage = NewStorage();

        var upload = await storage.PresignedPutUrlAsync(
            "checkins/abc/photo.jpg", PhotoTenMin, CancellationToken.None);

        var query = new Uri(upload.Url).Query;
        query.Should().Contain($"api_key={ApiKey}");
        query.Should().Contain("public_id=checkins%2Fabc%2Fphoto.jpg");
        query.Should().MatchRegex(@"timestamp=\d+");
        query.Should().MatchRegex(@"signature=[a-f0-9]{40}");
    }

    [Fact]
    public async Task PresignedPutUrl_signature_matches_cloudinary_algorithm()
    {
        var storage = NewStorage();

        var upload = await storage.PresignedPutUrlAsync(
            "checkins/abc/photo.jpg", PhotoTenMin, CancellationToken.None);

        // Cloudinary spec: signature = sha1("public_id=<id>&timestamp=<ts>" + api_secret).
        var timestamp = FixedNow.ToUnixTimeSeconds();
        var toSign = $"public_id=checkins/abc/photo.jpg&timestamp={timestamp}{ApiSecret}";
        var expected = System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes(toSign));
        var expectedHex = Convert.ToHexString(expected).ToLowerInvariant();

        new Uri(upload.Url).Query.Should().Contain($"signature={expectedHex}");
    }

    [Fact]
    public async Task PresignedPutUrl_reports_the_requested_ttl()
    {
        var storage = NewStorage();

        var upload = await storage.PresignedPutUrlAsync(
            "checkins/abc/photo.jpg", PhotoTenMin, CancellationToken.None);

        upload.ExpiresIn.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task PresignedGetUrl_returns_signed_delivery_url()
    {
        var storage = NewStorage();

        var url = await storage.PresignedGetUrlAsync(
            "checkins/abc/photo.jpg", TimeSpan.FromMinutes(5), CancellationToken.None);

        // Cloudinary signed delivery: https://res.cloudinary.com/{cloud}/image/upload/s--{sig}--/{public_id}
        url.Should().Contain($"/{Cloud}/image/upload/s--");
        url.Should().EndWith("checkins/abc/photo.jpg");
    }

    [Fact]
    public async Task DeleteAsync_calls_the_cloudinary_destroy_endpoint_with_signed_params()
    {
        _cloudinary
            .Given(Request.Create()
                .WithPath($"/v1_1/{Cloud}/image/destroy")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.OK)
                .WithBody("{\"result\":\"ok\"}"));

        var storage = NewStorage();

        await storage.DeleteAsync("checkins/abc/photo.jpg", CancellationToken.None);

        var log = _cloudinary.LogEntries.Should().ContainSingle().Subject;
        log.RequestMessage.Body.Should().Contain($"public_id=checkins%2Fabc%2Fphoto.jpg")
            .And.MatchRegex("signature=[a-f0-9]{40}");
    }

    [Fact]
    public async Task DeleteAsync_throws_when_cloudinary_returns_error_status()
    {
        _cloudinary
            .Given(Request.Create()
                .WithPath($"/v1_1/{Cloud}/image/destroy")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(HttpStatusCode.Unauthorized));

        var storage = NewStorage();

        await FluentActions
            .Invoking(() => storage.DeleteAsync("checkins/abc/photo.jpg", CancellationToken.None))
            .Should()
            .ThrowAsync<HttpRequestException>();
    }
}

file sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
