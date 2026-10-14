using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.Progression.Services;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Xp;

namespace Xpeak.Api.Tests.Progression;

public sealed class ProgressionServiceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public ProgressionServiceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public void Resolves_from_DI_and_delegates_the_arithmetic()
    {
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IProgressionService>();

        service.ComputeCheckInXp().Should().Be(XpCalculator.CheckInXp);
        service.XpForLevel(10).Should().Be(3_162);
        service.LevelForXp(3_162).Should().Be(10);
        service.EvaluateLevelUp(99, 100)
            .Should().Be(new LevelUpResult(true, 1, 1));
    }
}
