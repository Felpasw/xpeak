using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Social.Services;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.Social;

public sealed class CheckInVisibilityTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInVisibilityTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Open_visibility_returns_true_when_check_in_exists()
    {
        var owner = await CreateUserAsync("vis_owner");
        var caller = await CreateUserAsync("vis_caller");
        var checkInId = await InsertCheckInAsync(owner.Id);

        await using var scope = _factory.Services.CreateAsyncScope();
        var visibility = scope.ServiceProvider.GetRequiredService<ICheckInVisibility>();

        var canSee = await visibility.CanSeeAsync(caller.Id, checkInId);

        canSee.Should().BeTrue();
    }

    [Fact]
    public async Task Open_visibility_returns_false_when_check_in_does_not_exist()
    {
        var caller = await CreateUserAsync("vis_missing");

        await using var scope = _factory.Services.CreateAsyncScope();
        var visibility = scope.ServiceProvider.GetRequiredService<ICheckInVisibility>();

        var canSee = await visibility.CanSeeAsync(caller.Id, Guid.NewGuid());

        canSee.Should().BeFalse();
    }

    private async Task<AppUser> CreateUserAsync(string prefix)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var user = new AppUser { UserName = $"{prefix}_{suffix}", Email = $"{prefix}_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<Guid> InsertCheckInAsync(Guid userId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var id = Guid.NewGuid();
        db.CheckIns.Add(new CheckIn
        {
            Id = id,
            Title = "t",
            UserId = userId,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
        return id;
    }
}
