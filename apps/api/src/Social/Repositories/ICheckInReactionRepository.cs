namespace Xpeak.Api.Social.Repositories;

public sealed record ReactionAggregate(
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<string> Mine);

public sealed record ReactionUser(
    Guid Id,
    string Username,
    string? AvatarUrl);

public interface ICheckInReactionRepository
{
    /// <summary>Returns true when the triple was inserted, false when
    /// it already existed and was hard-deleted (toggle semantics).</summary>
    Task<bool> ToggleAsync(
        Guid checkInId,
        Guid userId,
        string emoji,
        CancellationToken ct = default);

    Task<ReactionAggregate> GetAggregateAsync(
        Guid checkInId,
        Guid callerId,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, IReadOnlyList<ReactionUser>>> GetUsersAsync(
        Guid checkInId,
        int perEmojiCap,
        CancellationToken ct = default);
}
