using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Return of <c>ICheckInService.CreateAsync</c>. Carries the fully
/// persisted <see cref="CheckIn"/>, the updated <see cref="AppUser"/>
/// snapshot (post-XP-update), the <see cref="LevelUpResult"/> for
/// this transaction and the derived <see cref="StreakInfo"/> scoped
/// to the check-in's group.
/// </summary>
public sealed record CreateCheckInResult(
    CheckIn CheckIn,
    AppUser User,
    LevelUpResult LevelUp,
    StreakInfo Streak);
