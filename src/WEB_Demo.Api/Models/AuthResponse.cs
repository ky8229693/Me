namespace WEB_Demo.Api.Models;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    AuthUser User);
