using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class RoleAdministrationService(
    RoleManager<IdentityRole> roleManager,
    UserManager<IdentityUser> userManager,
    RoleNameValidator nameValidator,
    AppDbContext dbContext,
    IUserSessionRevocationService sessionRevocation)
{
    public async Task<AdministrationResult<IReadOnlyCollection<AdminRole>>> GetRolesAsync()
    {
        var roles = await roleManager.Roles.AsNoTracking().OrderBy(role => role.Name).ToListAsync();
        var counts = await dbContext.UserRoles.AsNoTracking()
            .GroupBy(item => item.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count);
        var mappingRows = await dbContext.RolePermissions.AsNoTracking()
            .Where(item => AppPermissions.AllKeys.Contains(item.Permission.Key))
            .Select(item => new { item.RoleId, PermissionKey = item.Permission.Key })
            .ToArrayAsync();
        var mappings = mappingRows
            .GroupBy(item => item.RoleId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.PermissionKey).OrderBy(key => key).ToArray());
        return AdministrationResult<IReadOnlyCollection<AdminRole>>.Success(
            roles.Select(role => new AdminRole(
                role.Id,
                role.Name ?? string.Empty,
                AppRoles.IsSystemRole(role.Name),
                counts.GetValueOrDefault(role.Id),
                string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)
                    ? AppPermissions.AllKeys.OrderBy(key => key).ToArray()
                    : mappings.GetValueOrDefault(role.Id, []))).ToArray());
    }

    public async Task<AdministrationResult<AdminRole>> CreateAsync(string? name)
    {
        var validation = await nameValidator.ValidateAsync(name);
        if (validation.Status != AdministrationResultStatus.Success)
        {
            return ConvertFailure(validation);
        }

        var role = new IdentityRole(validation.Data!);
        var result = await roleManager.CreateAsync(role);
        return result.Succeeded
            ? AdministrationResult<AdminRole>.Success(ToAdminRole(role, 0), "Role created.")
            : MapIdentityFailure(result);
    }

    public async Task<AdministrationResult<AdminRole>> RenameAsync(string roleId, string? name)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return AdministrationResult<AdminRole>.NotFound("Role was not found.");
        if (AppRoles.IsSystemRole(role.Name)) return AdministrationResult<AdminRole>.Conflict("System roles cannot be renamed.");

        var validation = await nameValidator.ValidateAsync(name, role.Id);
        if (validation.Status != AdministrationResultStatus.Success) return ConvertFailure(validation);

        var userIds = await dbContext.UserRoles.Where(item => item.RoleId == role.Id)
            .Select(item => item.UserId).ToArrayAsync();
        role.Name = validation.Data;
        var update = await roleManager.UpdateAsync(role);
        if (!update.Succeeded) return await RollbackFailureAsync(transaction, update);

        foreach (var userId in userIds)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null) continue;
            var stamp = await userManager.UpdateSecurityStampAsync(user);
            if (!stamp.Succeeded) return await RollbackFailureAsync(transaction, stamp);
        }

        await CommitAsync(transaction);
        foreach (var userId in userIds)
        {
            await sessionRevocation.RevokeUserAsync(userId, "role-renamed");
        }

        return AdministrationResult<AdminRole>.Success(ToAdminRole(role, userIds.Length), "Role renamed.");
    }

    public async Task<AdministrationResult<AdminRole>> DeleteAsync(string roleId)
    {
        await using var transaction = await BeginTransactionIfSupportedAsync();
        var role = await roleManager.FindByIdAsync(roleId);
        if (role is null) return AdministrationResult<AdminRole>.NotFound("Role was not found.");
        if (AppRoles.IsSystemRole(role.Name)) return AdministrationResult<AdminRole>.Conflict("System roles cannot be deleted.");

        var count = await dbContext.UserRoles.CountAsync(item => item.RoleId == role.Id);
        if (count > 0) return AdministrationResult<AdminRole>.Conflict($"Role is assigned to {count} user(s) and cannot be deleted.");

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded) return await RollbackFailureAsync(transaction, result);
        await CommitAsync(transaction);
        return AdministrationResult<AdminRole>.Success(ToAdminRole(role, 0), "Role deleted.");
    }

    private static AdminRole ToAdminRole(IdentityRole role, int count) =>
        new(role.Id, role.Name ?? string.Empty, AppRoles.IsSystemRole(role.Name), count);

    private static AdministrationResult<AdminRole> ConvertFailure(AdministrationResult<string> result) => result.Status switch
    {
        AdministrationResultStatus.ValidationError => AdministrationResult<AdminRole>.Validation(result.Message),
        AdministrationResultStatus.Conflict => AdministrationResult<AdminRole>.Conflict(result.Message),
        _ => throw new InvalidOperationException("Unexpected role validation result.")
    };

    private static AdministrationResult<AdminRole> MapIdentityFailure(IdentityResult result)
    {
        var message = string.Join(" ", result.Errors.Select(error => error.Description));
        return result.Errors.Any(error => error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase))
            ? AdministrationResult<AdminRole>.Conflict(message)
            : AdministrationResult<AdminRole>.Validation(message);
    }

    private static async Task<AdministrationResult<AdminRole>> RollbackFailureAsync(
        IDbContextTransaction? transaction, IdentityResult result)
    {
        if (transaction is not null) await transaction.RollbackAsync();
        return AdministrationResult<AdminRole>.Conflict(
            string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync() =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

    private static Task CommitAsync(IDbContextTransaction? transaction) =>
        transaction?.CommitAsync() ?? Task.CompletedTask;
}
