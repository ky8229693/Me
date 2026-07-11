using Microsoft.AspNetCore.Identity;

namespace WEB_Demo.Api.Authorization;

public sealed class AppRoleInitializer(RoleManager<IdentityRole> roleManager)
{
    public async Task InitializeAsync()
    {
        foreach (var roleName in AppRoles.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new IdentityRole(roleName));
            if (!result.Succeeded)
            {
                var errors = string.Join(" ", result.Errors.Select(error => error.Description));
                throw new InvalidOperationException($"Unable to create role '{roleName}'. {errors}");
            }
        }
    }
}
