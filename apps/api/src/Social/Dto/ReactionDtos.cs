namespace Xpeak.Api.Social.Dto;

public sealed record ToggleReactionRequest(string Emoji);

public sealed record ReactionsResponse(
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<string> Mine);

public sealed record ReactionUserDto(
    Guid Id,
    string Username,
    string? AvatarUrl);

public sealed record ReactionsWithUsersResponse(
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<string> Mine,
    IReadOnlyDictionary<string, IReadOnlyList<ReactionUserDto>> Users);
