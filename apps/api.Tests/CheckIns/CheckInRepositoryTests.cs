using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xpeak.Api.CheckIns.Dto;
using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Repositories;
using Xpeak.Api.Infrastructure;
using Xpeak.Api.Tests.Support;
using Xpeak.Api.Users;

namespace Xpeak.Api.Tests.CheckIns;

public sealed class CheckInRepositoryTests
    : IClassFixture<XpeakWebApplicationFactory>
{
    private readonly XpeakWebApplicationFactory _factory;

    public CheckInRepositoryTests(XpeakWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task ListAsync_returns_only_the_callers_rows_ordered_newest_first()
    {
        var mine = await SeedUserAsync();
        var others = await SeedUserAsync();

        var now = DateTimeOffset.UtcNow;
        await InsertAsync(mine, now.AddDays(-2));
        await InsertAsync(mine, now.AddDays(-1));
        await InsertAsync(mine, now);
        await InsertAsync(others, now);

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page = await repo.ListAsync(mine.Id, limit: 10, cursor: null);

        page.Items.Should().HaveCount(3);
        page.Items.Select(i => i.CheckIn.UserId).Should().OnlyContain(id => id == mine.Id);
        page.Items.Should().BeInDescendingOrder(i => i.CheckIn.PerformedAt);
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task ListAsync_paginates_via_cursor_without_overlap()
    {
        var user = await SeedUserAsync();
        var now = DateTimeOffset.UtcNow;

        var inserted = new List<Guid>();
        for (var i = 0; i < 5; i++)
        {
            var id = await InsertAsync(user, now.AddDays(-i));
            inserted.Add(id);
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page1 = await repo.ListAsync(user.Id, limit: 2, cursor: null);
        page1.Items.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNull();

        var page2 = await repo.ListAsync(user.Id, limit: 2, cursor: page1.NextCursor);
        page2.Items.Should().HaveCount(2);
        page2.NextCursor.Should().NotBeNull();

        var page3 = await repo.ListAsync(user.Id, limit: 2, cursor: page2.NextCursor);
        page3.Items.Should().HaveCount(1);
        page3.NextCursor.Should().BeNull();

        var seen = page1.Items.Concat(page2.Items).Concat(page3.Items).Select(i => i.CheckIn.Id).ToList();
        seen.Should().OnlyHaveUniqueItems();
        seen.Should().HaveCount(5);
        seen.Should().BeEquivalentTo(inserted);
    }

    [Fact]
    public async Task ListAsync_returns_empty_page_when_user_has_no_check_ins()
    {
        var user = await SeedUserAsync();

        await using var scope = _factory.Services.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ICheckInRepository>();

        var page = await repo.ListAsync(user.Id, limit: 10, cursor: null);

        page.Items.Should().BeEmpty();
        page.NextCursor.Should().BeNull();
    }

    private async Task<AppUser> SeedUserAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..6];

        var user = new AppUser { UserName = $"lst_{suffix}", Email = $"lst_{suffix}@x.com" };
        (await users.CreateAsync(user, "hunter22!")).Succeeded.Should().BeTrue();
        return user;
    }

    private async Task<Guid> InsertAsync(AppUser user, DateTimeOffset at)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var id = Guid.NewGuid();
        db.CheckIns.Add(new CheckIn
        {
            Id = id,
            Title = "test",
            UserId = user.Id,
            XpEarned = 10,
            ScoringSnapshot = new ScoringSnapshot(10, [], 10),
            PerformedAt = at,
        });
        await db.SaveChangesAsync();
        return id;
    }
}
