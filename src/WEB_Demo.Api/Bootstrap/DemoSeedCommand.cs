namespace WEB_Demo.Api.Bootstrap;

public sealed record DemoSeedCommand(string OwnerEmail)
{
    public static bool IsRequested(string[] args) =>
        args.Length > 0 && string.Equals(args[0], "seed-demo-data", StringComparison.OrdinalIgnoreCase);

    public static bool TryParse(string[] args, out DemoSeedCommand? command, out string error)
    {
        command = null;
        error = string.Empty;
        string? ownerEmail = null;
        for (var index = 1; index < args.Length; index++)
        {
            if (args[index] == "--owner-email" && index + 1 < args.Length)
            {
                ownerEmail = args[++index];
                continue;
            }

            error = $"Unknown or incomplete argument: {args[index]}";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ownerEmail))
        {
            error = "Usage: seed-demo-data --owner-email <address>";
            return false;
        }

        command = new DemoSeedCommand(ownerEmail.Trim());
        return true;
    }
}
