using System.Text.RegularExpressions;

namespace WEB_Demo.Api.Authorization;

public sealed record AppPermission(string Key, string Name, string Description, string Category);

public static partial class AppPermissions
{
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";
    public const string PermissionsManage = "permissions.manage";
    public const string TodosRead = "todos.read";
    public const string TodosManage = "todos.manage";

    public static readonly IReadOnlyList<AppPermission> Catalog =
    [
        new(UsersRead, "View users", "View the user directory and account details.", "Users"),
        new(UsersManage, "Manage users", "Enable accounts and assign roles.", "Users"),
        new(RolesRead, "View roles", "View roles and their permission summary.", "Roles"),
        new(RolesManage, "Manage roles", "Create, rename, and delete custom roles.", "Roles"),
        new(PermissionsManage, "Manage permissions", "View the permission catalog and configure role permissions.", "Permissions"),
        new(TodosRead, "View todos", "View todo items.", "Todos"),
        new(TodosManage, "Manage todos", "Create, update, and delete todo items.", "Todos")
    ];

    private static readonly HashSet<string> Keys = Catalog.Select(item => item.Key).ToHashSet(StringComparer.Ordinal);
    public static bool IsKnown(string key) => Keys.Contains(key);
    public static IReadOnlyCollection<string> AllKeys => Keys;

    public static void Validate()
    {
        if (Catalog.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() != Catalog.Count)
            throw new InvalidOperationException("Permission catalog contains duplicate keys.");
        if (Catalog.Any(item => !PermissionKeyPattern().IsMatch(item.Key)))
            throw new InvalidOperationException("Permission catalog contains an invalid key.");
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*\\.[a-z][a-z0-9-]*$")]
    private static partial Regex PermissionKeyPattern();
}
