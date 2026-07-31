using System.Globalization;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;

namespace WEB_Demo.Api.Services;

public sealed class RoleNameValidator(RoleManager<IdentityRole> roleManager)
{
    public async Task<AdministrationResult<string>> ValidateAsync(string? name, string? currentRoleId = null)
    {
        var value = name?.Trim() ?? string.Empty;
        if (value.Length is < 2 or > 50)
        {
            return AdministrationResult<string>.Validation("Role name must contain between 2 and 50 characters.");
        }

        if (value.Any(character =>
                !char.IsLetterOrDigit(character) &&
                !char.IsWhiteSpace(character) &&
                character is not '-' and not '_'))
        {
            return AdministrationResult<string>.Validation(
                "Role name may only contain letters, numbers, spaces, hyphens, and underscores.");
        }

        if (value.Any(character => char.GetUnicodeCategory(character) is
                UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or
                UnicodeCategory.ParagraphSeparator))
        {
            return AdministrationResult<string>.Validation("Role name contains unsupported characters.");
        }

        if (AppRoles.IsSystemRole(value))
        {
            return AdministrationResult<string>.Validation("Admin and User are reserved role names.");
        }

        var existing = await roleManager.FindByNameAsync(value);
        if (existing is not null && !string.Equals(existing.Id, currentRoleId, StringComparison.Ordinal))
        {
            return AdministrationResult<string>.Conflict("A role with the same name already exists.");
        }

        return AdministrationResult<string>.Success(value);
    }
}
