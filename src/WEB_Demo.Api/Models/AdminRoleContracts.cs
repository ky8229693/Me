namespace WEB_Demo.Api.Models;

public sealed record AdminRole(string Id, string Name, bool IsSystem, int UserCount);

public sealed record CreateRoleRequest(string? Name);

public sealed record UpdateRoleRequest(string? Name);
