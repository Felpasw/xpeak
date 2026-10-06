using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Progression.Entities;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Return of <c>ICheckInService.CreateAsync</c>. Carries the fully
/// persisted <see cref="CheckIn"/>, the updated <see cref="AppUser"/>
/// snapshot (post-XP-update), the <see cref="LevelUpResult"/> for
/// this transaction, the derived <see cref="StreakInfo"/> scoped
/// to the check-in's group, and the <see cref="Category"/> picked
/// by the client — the endpoint turns it into a <c>CategorySnapshot</c>
/// to keep the DTO property-direct.
/// </summary>
public sealed record CreateCheckInResult(
    CheckIn CheckIn,
    AppUser User,
    LevelUpResult LevelUp,
    StreakInfo Streak,
    Category Category);
