namespace WEB_Demo.Api.Data;

public sealed class Permission
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Category { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
