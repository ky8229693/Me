namespace WEB_Demo.Api.Models;

public sealed record AdminRole(
    string Id,
    string Name,
    bool IsSystem,
    int UserCount,
    IReadOnlyCollection<string>? Permissions = null);

public sealed record CreateRoleRequest(string? Name);

public sealed record UpdateRoleRequest(string? Name);
