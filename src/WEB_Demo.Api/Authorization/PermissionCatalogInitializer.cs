using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Authorization;

public sealed class PermissionCatalogInitializer(AppDbContext dbContext, RoleManager<IdentityRole> roleManager)
{
    public async Task InitializeAsync()
    {
        AppPermissions.Validate();
        var existing = await dbContext.Permissions.ToDictionaryAsync(item => item.Key, StringComparer.Ordinal);
        foreach (var definition in AppPermissions.Catalog)
        {
            if (!existing.TryGetValue(definition.Key, out var permission))
            {
                dbContext.Permissions.Add(new Permission
                {
                    Key = definition.Key, Name = definition.Name,
                    Description = definition.Description, Category = definition.Category
                });
            }
            else
            {
                permission.Name = definition.Name;
                permission.Description = definition.Description;
                permission.Category = definition.Category;
            }
        }
        await dbContext.SaveChangesAsync();

        var admin = await roleManager.FindByNameAsync(AppRoles.Admin)
            ?? throw new InvalidOperationException("Admin role must exist before permission initialization.");
        var permissionIds = await dbContext.Permissions.Where(item => AppPermissions.AllKeys.Contains(item.Key))
            .Select(item => item.Id).ToArrayAsync();
        var assigned = (await dbContext.RolePermissions.Where(item => item.RoleId == admin.Id)
            .Select(item => item.PermissionId).ToArrayAsync()).ToHashSet();
        dbContext.RolePermissions.AddRange(permissionIds.Where(id => !assigned.Contains(id))
            .Select(id => new RolePermission { RoleId = admin.Id, PermissionId = id }));
        await dbContext.SaveChangesAsync();
    }
}
