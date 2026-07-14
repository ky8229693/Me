using System.Text.RegularExpressions;

namespace WEB_Demo.Api.E2E;

public static partial class E2EDatabaseGuard
{
    private static readonly HashSet<string> ForbiddenNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "web_demo",
        "postgres",
        "template0",
        "template1"
    };

    public static bool TryValidate(string environmentName, string databaseName, out string error)
    {
        if (!string.Equals(environmentName, "E2E", StringComparison.Ordinal))
        {
            error = "E2E database lifecycle commands require the E2E environment.";
            return false;
        }

        if (ForbiddenNames.Contains(databaseName) || !AllowedDatabaseName().IsMatch(databaseName))
        {
            error = "The database name is not an allowed E2E database target.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    [GeneratedRegex("^web_demo_e2e_[a-z0-9][a-z0-9_]{7,49}$", RegexOptions.CultureInvariant)]
    private static partial Regex AllowedDatabaseName();
}
