using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Data;

public sealed class RolePermission
{
    public required string RoleId { get; set; }
    public int PermissionId { get; set; }
    public IdentityRole Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
