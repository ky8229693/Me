using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Bootstrap;

public sealed class AdminBootstrapService(
    UserManager<IdentityUser> userManager,
    IJwtSessionRegistry sessionRegistry)
{
    public async Task<AdminBootstrapResult> BootstrapAsync(
        string email,
        string? password,
        bool confirmExistingUser)
    {
        email = email.Trim();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return AdminBootstrapResult.Failure("A valid email is required.");
        }

        var user = await userManager.FindByEmailAsync(email);
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        if (user is not null && admins.Any(admin => admin.Id == user.Id))
        {
            return AdminBootstrapResult.Success($"Admin account is already configured for {email}.");
        }

        if (admins.Count > 0)
        {
            return AdminBootstrapResult.Failure("An Admin account already exists. Use the authenticated administration API.");
        }

        var created = false;
        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return AdminBootstrapResult.Failure("A password is required when creating a new Admin account.");
            }

            user = new IdentityUser { UserName = email, Email = email };
            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                return AdminBootstrapResult.Failure(string.Join(" ", createResult.Errors.Select(error => error.Description)));
            }

            created = true;
        }
        else
        {
            if (!confirmExistingUser)
            {
                return AdminBootstrapResult.Failure("The account already exists. Confirm promotion explicitly.");
            }

            if (await userManager.IsLockedOutAsync(user))
            {
                return AdminBootstrapResult.Failure("The existing account is disabled and cannot be promoted.");
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        var rolesToAdd = new[] { AppRoles.User, AppRoles.Admin }.Except(roles, StringComparer.Ordinal).ToArray();
        var roleResult = rolesToAdd.Length == 0
            ? IdentityResult.Success
            : await userManager.AddToRolesAsync(user, rolesToAdd);

        if (!roleResult.Succeeded)
        {
            if (created)
            {
                await userManager.DeleteAsync(user);
            }

            return AdminBootstrapResult.Failure(string.Join(" ", roleResult.Errors.Select(error => error.Description)));
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            if (created)
            {
                await userManager.DeleteAsync(user);
            }

            return AdminBootstrapResult.Failure("Unable to finalize Admin bootstrap.");
        }

        sessionRegistry.RevokeAll(user.Id);

        return AdminBootstrapResult.Success(created
            ? $"Admin account created for {email}."
            : $"Existing account promoted to Admin for {email}.");
    }
}
