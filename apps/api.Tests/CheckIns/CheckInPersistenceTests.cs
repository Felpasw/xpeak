using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CheckInPersistenceTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInPersistenceTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task Happy_path_persists_check_in_with_scoring_snapshot()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = new CheckIn
        {
            Id = Guid.NewGuid(),
            Title = "test",
            UserId = user.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = DateTimeOffset.UtcNow,
            DurationMinutes = 45,
            Notes = "leg day",
        };
        db.CheckIns.Add(checkIn);
        await db.SaveChangesAsync();

        var reloaded = await db.CheckIns
            .AsNoTracking()
            .SingleAsync(c => c.Id == checkIn.Id);

        reloaded.XpEarned.Should().Be(10);
        reloaded.Notes.Should().Be("leg day");
        reloaded.ScoringSnapshot.BaseXp.Should().Be(10);
        reloaded.ScoringSnapshot.Total.Should().Be(10);
        reloaded.ScoringSnapshot.Multipliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Rejects_zero_duration_minutes()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(user);
        checkIn.DurationMinutes = 0;
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Rejects_notes_over_280_characters()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(user);
        checkIn.Notes = new string('a', 281);
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Accepts_zero_xp_earned_for_pending_rows()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(user);
        checkIn.XpEarned = 0;
        checkIn.ScoringSnapshot = new ScoringSnapshot(0, [], 0);

        db.CheckIns.Add(checkIn);
        var act = async () => await db.SaveChangesAsync();

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Rejects_unknown_user_id_fk()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var checkIn = NewCheckIn(user);
        checkIn.UserId = Guid.NewGuid();
        db.CheckIns.Add(checkIn);

        var act = async () => await db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>();
    }

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"ci_{suffix}", Email = $"ci_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private static CheckIn NewCheckIn(AppUser user) => new()
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Title = "test",
        XpEarned = 10,
        ScoringSnapshot = new ScoringSnapshot(10, [], 10),
        PerformedAt = DateTimeOffset.UtcNow,
    };
}
