using Xpeak.Api.Social.Entities;

namespace Xpeak.Api.Social.Repositories;

public sealed record CommentAuthor(
    Guid Id,
    string Username,
    string? AvatarUrl);

public sealed record CommentRow(
    CheckInComment Comment,
    CommentAuthor Author);

public sealed record CommentPage(
    IReadOnlyList<CommentRow> Items,
    string? NextCursor);

public interface ICheckInCommentRepository
{
    Task<CommentRow> AddAsync(
        Guid checkInId,
        Guid userId,
        string body,
        CancellationToken ct = default);

    Task<CommentRow?> GetByIdAsync(
        Guid commentId,
        CancellationToken ct = default);

    /// <summary>Returns the row with <c>DeletedAt</c> stamped, or
    /// null when the comment does not exist or was already
    /// soft-deleted.</summary>
    Task<CheckInComment?> SoftDeleteAsync(
        Guid commentId,
        CancellationToken ct = default);

    Task<CommentPage> ListPageAsync(
        Guid checkInId,
        string? cursor,
        int limit,
        CancellationToken ct = default);

    Task<int> CountVisibleAsync(
        Guid checkInId,
        CancellationToken ct = default);
}
