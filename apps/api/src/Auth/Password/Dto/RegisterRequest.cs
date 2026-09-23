namespace Xpeak.Api.Auth.Password;

public sealed record RegisterRequest(string Username, string Email, string Password);
