namespace WEB_Demo.Api.Bootstrap;

public sealed record AdminBootstrapCommand(
    string Email,
    string? PasswordEnvironmentVariable,
    bool ConfirmExistingUser)
{
    public static bool IsRequested(string[] args) =>
        args.Length > 0 && string.Equals(args[0], "bootstrap-admin", StringComparison.OrdinalIgnoreCase);

    public static bool TryParse(string[] args, out AdminBootstrapCommand? command, out string error)
    {
        command = null;
        error = string.Empty;
        string? email = null;
        string? passwordEnvironmentVariable = null;
        var confirmExisting = false;

        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--email" when index + 1 < args.Length:
                    email = args[++index];
                    break;
                case "--password-env" when index + 1 < args.Length:
                    passwordEnvironmentVariable = args[++index];
                    break;
                case "--confirm-existing":
                    confirmExisting = true;
                    break;
                default:
                    error = $"Unknown or incomplete argument: {args[index]}";
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            error = "Usage: bootstrap-admin --email <address> [--password-env <name>] [--confirm-existing]";
            return false;
        }

        command = new AdminBootstrapCommand(email, passwordEnvironmentVariable, confirmExisting);
        return true;
    }

    public string? ReadPassword()
    {
        if (!string.IsNullOrWhiteSpace(PasswordEnvironmentVariable))
        {
            return Environment.GetEnvironmentVariable(PasswordEnvironmentVariable);
        }

        if (Console.IsInputRedirected)
        {
            return null;
        }

        Console.Write("Password (hidden): ");
        var password = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return new string(password.ToArray());
            }

            if (key.Key == ConsoleKey.Backspace && password.Count > 0)
            {
                password.RemoveAt(password.Count - 1);
            }
            else if (!char.IsControl(key.KeyChar))
            {
                password.Add(key.KeyChar);
            }
        }
    }
}
