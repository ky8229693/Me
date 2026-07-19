namespace WEB_Demo.Api.Models;

public sealed record PermissionInfo(string Key, string Name, string Description, string Category);
public sealed record RolePermissions(string RoleId, string RoleName, bool IsSystem, IReadOnlyCollection<string> Permissions);
public sealed record UpdateRolePermissionsRequest(IReadOnlyCollection<string>? Permissions);
