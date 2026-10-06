using Xpeak.Api.Media.Dto;
using Xpeak.Api.Media.Providers.Fake;

namespace Xpeak.Api.Tests.Media;

public sealed class FakeMediaStorageTests
{
    private static readonly PresignOptions PhotoTenMin =
        new("image/jpeg", 1_024, TimeSpan.FromMinutes(10));

    [Fact]
    public async Task PresignedPutUrl_returns_deterministic_url_scoped_to_key_and_ttl()
    {
        var storage = new FakeMediaStorage();

        var result = await storage.PresignedPutUrlAsync(
            "checkins/abc/photo.jpg",
            PhotoTenMin,
            CancellationToken.None);

        result.Url.Should().Be("https://fake.local/put/checkins/abc/photo.jpg?ttl=600");
        result.ExpiresIn.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public async Task PresignedPutUrl_records_each_key_in_order()
    {
        var storage = new FakeMediaStorage();

        await storage.PresignedPutUrlAsync("k1", PhotoTenMin, CancellationToken.None);
        await storage.PresignedPutUrlAsync("k2", PhotoTenMin, CancellationToken.None);

        storage.PutCalls.Should().Equal("k1", "k2");
    }

    [Fact]
    public async Task PresignedGetUrl_returns_deterministic_url_scoped_to_key_and_ttl()
    {
        var storage = new FakeMediaStorage();

        var url = await storage.PresignedGetUrlAsync(
            "checkins/abc/photo.jpg",
            TimeSpan.FromMinutes(5),
            CancellationToken.None);

        url.Should().Be("https://fake.local/get/checkins/abc/photo.jpg?ttl=300");
    }

    [Fact]
    public async Task Delete_records_the_key()
    {
        var storage = new FakeMediaStorage();

        await storage.DeleteAsync("checkins/abc/photo.jpg", CancellationToken.None);

        storage.DeleteCalls.Should().ContainSingle().Which.Should().Be("checkins/abc/photo.jpg");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PresignedPutUrl_rejects_blank_keys(string key)
    {
        var storage = new FakeMediaStorage();

        await FluentActions
            .Invoking(() => storage.PresignedPutUrlAsync(key, PhotoTenMin, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task PresignedPutUrl_rejects_non_positive_byte_size()
    {
        var storage = new FakeMediaStorage();
        var invalid = new PresignOptions("image/jpeg", 0, TimeSpan.FromMinutes(10));

        await FluentActions
            .Invoking(() => storage.PresignedPutUrlAsync("k", invalid, CancellationToken.None))
            .Should()
            .ThrowAsync<ArgumentOutOfRangeException>();
    }
}
