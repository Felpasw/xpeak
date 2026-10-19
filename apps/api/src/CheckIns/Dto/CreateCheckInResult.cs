using Xpeak.Api.CheckIns.Entities;
using Xpeak.Api.CheckIns.Services;
using Xpeak.Api.Users;
using Xpeak.Api.Xp;

namespace Xpeak.Api.CheckIns.Dto;

/// <summary>
/// Return of <c>ICheckInService.CreateAsync</c>. Carries the persisted
/// <see cref="CheckIn"/>, the updated <see cref="AppUser"/> snapshot,
/// the level-up delta for this transaction and the derived per-user
/// <see cref="StreakInfo"/>.
/// </summary>
public sealed record CreateCheckInResult(
    CheckIn CheckIn,
    AppUser User,
    LevelUpResult LevelUp,
    StreakInfo Streak);
