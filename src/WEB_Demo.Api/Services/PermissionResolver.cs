using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.Services;

public interface IPermissionResolver
{
    Task<IReadOnlyCollection<string>> GetForUserAsync(IdentityUser user);
    Task<IReadOnlyCollection<string>> GetForRolesAsync(IEnumerable<string> roles);
}

public sealed class PermissionResolver(AppDbContext dbContext, UserManager<IdentityUser> userManager) : IPermissionResolver
{
    public async Task<IReadOnlyCollection<string>> GetForUserAsync(IdentityUser user) =>
        await GetForRolesAsync(await userManager.GetRolesAsync(user));

    public async Task<IReadOnlyCollection<string>> GetForRolesAsync(IEnumerable<string> roles)
    {
        var roleNames = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (roleNames.Contains(AppRoles.Admin, StringComparer.OrdinalIgnoreCase))
            return AppPermissions.AllKeys.OrderBy(key => key).ToArray();

        var knownKeys = AppPermissions.AllKeys.ToArray();
        return await dbContext.RolePermissions.AsNoTracking()
            .Where(mapping => roleNames.Contains(mapping.Role.Name!))
            .Select(mapping => mapping.Permission.Key)
            .Where(key => knownKeys.Contains(key))
            .Distinct().OrderBy(key => key).ToArrayAsync();
    }
}
