namespace WEB_Demo.Api.E2E;

public sealed record E2EDatabaseCommand(
    string Action,
    string DatabaseName,
    string? AdminEmail,
    string? PasswordEnvironmentVariable)
{
    public static bool IsRequested(string[] args) =>
        args.Length > 0 && string.Equals(args[0], "e2e-database", StringComparison.OrdinalIgnoreCase);

    public static bool TryParse(string[] args, out E2EDatabaseCommand? command, out string error)
    {
        command = null;
        error = string.Empty;

        if (args.Length < 2 || args[1] is not ("prepare" or "cleanup"))
        {
            error = Usage;
            return false;
        }

        string? databaseName = null;
        string? adminEmail = null;
        string? passwordEnvironmentVariable = null;

        for (var index = 2; index < args.Length; index++)
        {
            if (index + 1 >= args.Length)
            {
                error = Usage;
                return false;
            }

            var value = args[++index];
            switch (args[index - 1])
            {
                case "--database":
                    databaseName = value;
                    break;
                case "--admin-email":
                    adminEmail = value;
                    break;
                case "--password-env":
                    passwordEnvironmentVariable = value;
                    break;
                default:
                    error = Usage;
                    return false;
            }
        }

        var action = args[1];
        if (string.IsNullOrWhiteSpace(databaseName) ||
            action == "prepare" && (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(passwordEnvironmentVariable)))
        {
            error = Usage;
            return false;
        }

        command = new E2EDatabaseCommand(action, databaseName, adminEmail, passwordEnvironmentVariable);
        return true;
    }

    public string? ReadPassword() => PasswordEnvironmentVariable is null
        ? null
        : Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);

    private const string Usage =
        "Usage: e2e-database <prepare|cleanup> --database <web_demo_e2e_name> " +
        "[--admin-email <address> --password-env <name>]";
}
