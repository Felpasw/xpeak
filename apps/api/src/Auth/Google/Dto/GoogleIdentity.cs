namespace Xpeak.Api.Auth.Google;

/// <summary>
/// Immutable snapshot of the fields we consume from Google's OAuth
/// identity payload. Used as the input to <see cref="GoogleAccountLinker"/>
/// so the linker never depends on ASP.NET's authentication types.
/// </summary>
public sealed record GoogleIdentity(
    string GoogleUid,
    string Email,
    string? AvatarUrl,
    string UsernameSeed);
