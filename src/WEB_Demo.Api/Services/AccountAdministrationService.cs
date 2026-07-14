using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public sealed class AccountAdministrationService(
    UserManager<IdentityUser> userManager,
    AppDbContext dbContext)
{
    public const int DefaultPageSize = 10;
    public const int MaximumPageSize = 100;

    public async Task<AdministrationResult<PagedResult<AdminUser>>> GetUsersAsync(
        int page,
        int pageSize,
        string? search)
    {
        if (page < 1 || pageSize < 1 || pageSize > MaximumPageSize)
        {
            return AdministrationResult<PagedResult<AdminUser>>.Validation(
                $"Page must be at least 1 and pageSize must be between 1 and {MaximumPageSize}.");
        }

        var query = userManager.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToUpperInvariant();
            query = query.Where(user =>
                user.NormalizedEmail != null && user.NormalizedEmail.Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderBy(user => user.NormalizedEmail)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        var items = new List<AdminUser>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToAdminUserAsync(user));
        }

        return AdministrationResult<PagedResult<AdminUser>>.Success(
            new PagedResult<AdminUser>(items, page, pageSize, totalCount));
    }

    public async Task<AdministrationResult<AdminUser>> GetUserAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is null
            ? AdministrationResult<AdminUser>.NotFound("User was not found.")
            : AdministrationResult<AdminUser>.Success(await ToAdminUserAsync(user));
    }

    public async Task<AdministrationResult<AdminUser>> SetEnabledAsync(
        string actorUserId,
        string targetUserId,
        bool isEnabled)
    {
        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return AdministrationResult<AdminUser>.NotFound("User was not found.");
        }

        if (!isEnabled && string.Equals(actorUserId, targetUserId, StringComparison.Ordinal))
        {
            return AdministrationResult<AdminUser>.Conflict("You cannot disable your own account.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync();
        if (!isEnabled && await userManager.IsInRoleAsync(user, AppRoles.Admin) &&
            await CountEnabledAdminsAsync() <= 1)
        {
            return AdministrationResult<AdminUser>.Conflict("The last enabled Admin cannot be disabled.");
        }

        var lockoutResult = await userManager.SetLockoutEnabledAsync(user, true);
        if (!lockoutResult.Succeeded)
        {
            return AdministrationResult<AdminUser>.Conflict(JoinErrors(lockoutResult));
        }

        lockoutResult = await userManager.SetLockoutEndDateAsync(
            user,
            isEnabled ? null : DateTimeOffset.UtcNow.AddYears(100));
        if (!lockoutResult.Succeeded)
        {
            await RollbackAsync(transaction);
            return AdministrationResult<AdminUser>.Conflict(JoinErrors(lockoutResult));
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            await RollbackAsync(transaction);
            return AdministrationResult<AdminUser>.Conflict(JoinErrors(stampResult));
        }

        await CommitAsync(transaction);
        return AdministrationResult<AdminUser>.Success(
            await ToAdminUserAsync(user),
            isEnabled ? "Account enabled." : "Account disabled.");
    }

    public async Task<AdministrationResult<AdminUser>> SetRolesAsync(
        string actorUserId,
        string targetUserId,
        IReadOnlyCollection<string>? requestedRoles)
    {
        if (requestedRoles is null)
        {
            return AdministrationResult<AdminUser>.Validation("Roles are required.");
        }

        var desiredRoles = requestedRoles.ToHashSet(StringComparer.Ordinal);
        var allowedRoles = new HashSet<string>([AppRoles.Admin, AppRoles.User], StringComparer.Ordinal);
        if (!desiredRoles.Contains(AppRoles.User))
        {
            return AdministrationResult<AdminUser>.Validation("Every account must retain the User role.");
        }

        if (!desiredRoles.IsSubsetOf(allowedRoles))
        {
            return AdministrationResult<AdminUser>.Validation("One or more roles are not supported.");
        }

        var user = await userManager.FindByIdAsync(targetUserId);
        if (user is null)
        {
            return AdministrationResult<AdminUser>.NotFound("User was not found.");
        }

        var currentRoles = (await userManager.GetRolesAsync(user)).ToHashSet(StringComparer.Ordinal);
        var removesAdmin = currentRoles.Contains(AppRoles.Admin) && !desiredRoles.Contains(AppRoles.Admin);
        if (removesAdmin && string.Equals(actorUserId, targetUserId, StringComparison.Ordinal))
        {
            return AdministrationResult<AdminUser>.Conflict("You cannot remove your own Admin role.");
        }

        if (currentRoles.SetEquals(desiredRoles))
        {
            return AdministrationResult<AdminUser>.Success(await ToAdminUserAsync(user), "Roles are unchanged.");
        }

        await using var transaction = await BeginTransactionIfSupportedAsync();
        if (removesAdmin && !await userManager.IsLockedOutAsync(user) && await CountEnabledAdminsAsync() <= 1)
        {
            return AdministrationResult<AdminUser>.Conflict("The last enabled Admin role cannot be removed.");
        }

        var toAdd = desiredRoles.Except(currentRoles, StringComparer.Ordinal).ToArray();
        var toRemove = currentRoles.Except(desiredRoles, StringComparer.Ordinal).ToArray();
        if (toAdd.Length > 0)
        {
            var addResult = await userManager.AddToRolesAsync(user, toAdd);
            if (!addResult.Succeeded)
            {
                await RollbackAsync(transaction);
                return AdministrationResult<AdminUser>.Conflict(JoinErrors(addResult));
            }
        }

        if (toRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removeResult.Succeeded)
            {
                await RollbackAsync(transaction);
                return AdministrationResult<AdminUser>.Conflict(JoinErrors(removeResult));
            }
        }

        var stampResult = await userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            await RollbackAsync(transaction);
            return AdministrationResult<AdminUser>.Conflict(JoinErrors(stampResult));
        }

        await CommitAsync(transaction);
        return AdministrationResult<AdminUser>.Success(await ToAdminUserAsync(user), "Roles updated.");
    }

    private async Task<AdminUser> ToAdminUserAsync(IdentityUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AdminUser(
            user.Id,
            user.Email ?? string.Empty,
            !await userManager.IsLockedOutAsync(user),
            roles.OrderBy(role => role, StringComparer.Ordinal).ToArray());
    }

    private async Task<int> CountEnabledAdminsAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var count = 0;
        foreach (var admin in admins)
        {
            if (!await userManager.IsLockedOutAsync(admin))
            {
                count++;
            }
        }

        return count;
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync() =>
        dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable)
            : null;

    private static Task CommitAsync(IDbContextTransaction? transaction) =>
        transaction?.CommitAsync() ?? Task.CompletedTask;

    private static Task RollbackAsync(IDbContextTransaction? transaction) =>
        transaction?.RollbackAsync() ?? Task.CompletedTask;

    private static string JoinErrors(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(error => error.Description));
}
