namespace WEB_Demo.Api.Models;

public sealed record AuthUser(
    string Id,
    string Email,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);
