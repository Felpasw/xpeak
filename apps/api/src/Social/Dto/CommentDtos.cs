namespace Xpeak.Api.Social.Dto;

public sealed record CreateCommentRequest(string Body);

public sealed record CommentAuthorDto(
    Guid Id,
    string Username,
    string? AvatarUrl);

public sealed record CommentResponse(
    Guid Id,
    Guid CheckInId,
    CommentAuthorDto Author,
    string Body,
    DateTimeOffset CreatedAt);

public sealed record CommentsPageResponse(
    IReadOnlyList<CommentResponse> Items,
    string? NextCursor);
