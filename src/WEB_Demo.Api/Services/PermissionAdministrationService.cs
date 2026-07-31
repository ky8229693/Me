using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class PermissionAdministrationService(
    AppDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    IUserSessionRevocationService sessionRevocation)
{
    public IReadOnlyCollection<PermissionInfo> GetCatalog() => AppPermissions.Catalog
        .Select(item => new PermissionInfo(item.Key, item.Name, item.Description, item.Category)).ToArray();

    public async Task<AdministrationResult<RolePermissions>> GetRoleAsync(string roleId)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return AdministrationResult<RolePermissions>.NotFound("Role was not found.");
        return AdministrationResult<RolePermissions>.Success(await ToContractAsync(role));
    }

    public async Task<AdministrationResult<RolePermissions>> SetRoleAsync(string roleId, IReadOnlyCollection<string>? requested)
    {
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return AdministrationResult<RolePermissions>.NotFound("Role was not found.");
        if (requested is null) return AdministrationResult<RolePermissions>.Validation("Permissions are required.");

        var desired = requested.Where(key => !string.IsNullOrWhiteSpace(key)).Select(key => key.Trim())
            .ToHashSet(StringComparer.Ordinal);
        if (desired.Any(key => !AppPermissions.IsKnown(key)))
            return AdministrationResult<RolePermissions>.Validation("One or more permissions do not exist.");
        if (string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase) &&
            !desired.SetEquals(AppPermissions.AllKeys))
            return AdministrationResult<RolePermissions>.Conflict("Admin permissions cannot be reduced.");

        var current = await dbContext.RolePermissions.Where(item => item.RoleId == role.Id)
            .Include(item => item.Permission).ToListAsync();
        if (current.Select(item => item.Permission.Key).ToHashSet(StringComparer.Ordinal).SetEquals(desired))
            return AdministrationResult<RolePermissions>.Success(await ToContractAsync(role), "Permissions are unchanged.");

        var permissions = await dbContext.Permissions.Where(item => desired.Contains(item.Key)).ToDictionaryAsync(item => item.Key);
        var userIds = await dbContext.UserRoles.Where(item => item.RoleId == role.Id).Select(item => item.UserId).ToArrayAsync();

        await using var transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync()
            : null;
        try
        {
            dbContext.RolePermissions.RemoveRange(current.Where(item => !desired.Contains(item.Permission.Key)));
            var currentKeys = current.Select(item => item.Permission.Key).ToHashSet(StringComparer.Ordinal);
            dbContext.RolePermissions.AddRange(desired.Except(currentKeys).Select(key =>
                new RolePermission { RoleId = role.Id, PermissionId = permissions[key].Id }));
            foreach (var userId in userIds)
            {
                var user = await userManager.FindByIdAsync(userId) ?? throw new InvalidOperationException("Assigned user was not found.");
                var result = await userManager.UpdateSecurityStampAsync(user);
                if (!result.Succeeded) throw new InvalidOperationException("Unable to update user security stamp.");
            }
            await dbContext.SaveChangesAsync();
            if (transaction is not null) await transaction.CommitAsync();
        }
        catch
        {
            if (transaction is not null) await transaction.RollbackAsync();
            return AdministrationResult<RolePermissions>.Conflict("Permission update failed and was rolled back.");
        }

        foreach (var userId in userIds) await sessionRevocation.RevokeUserAsync(userId, "role-permissions-changed");
        return AdministrationResult<RolePermissions>.Success(await ToContractAsync(role), "Permissions updated.");
    }

    private async Task<RolePermissions> ToContractAsync(IdentityRole role)
    {
        var keys = string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
            ? AppPermissions.AllKeys.OrderBy(key => key).ToArray()
            : await dbContext.RolePermissions.AsNoTracking().Where(item => item.RoleId == role.Id)
                .Select(item => item.Permission.Key).Where(key => AppPermissions.AllKeys.Contains(key)).OrderBy(key => key).ToArrayAsync();
        return new RolePermissions(role.Id, role.Name ?? string.Empty, AppRoles.IsSystemRole(role.Name), keys);
    }
}
