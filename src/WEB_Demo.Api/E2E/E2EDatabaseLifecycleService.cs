using Microsoft.EntityFrameworkCore;
using Npgsql;
using WEB_Demo.Api.Data;

namespace WEB_Demo.Api.E2E;

public sealed class E2EDatabaseLifecycleService(IConfiguration configuration, IHostEnvironment environment)
{
    public async Task<E2EDatabaseResult> PrepareAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (!TryGetConnections(databaseName, out var maintenanceConnection, out var targetConnection, out var error))
        {
            return E2EDatabaseResult.Failure(error);
        }

        try
        {
            await using var connection = new NpgsqlConnection(maintenanceConnection);
            await connection.OpenAsync(cancellationToken);

            await using (var existsCommand = new NpgsqlCommand(
                "SELECT EXISTS(SELECT 1 FROM pg_database WHERE datname = @databaseName)", connection))
            {
                existsCommand.Parameters.AddWithValue("databaseName", databaseName);
                if (await existsCommand.ExecuteScalarAsync(cancellationToken) is true)
                {
                    return E2EDatabaseResult.Failure("The E2E database already exists; a fresh database is required.");
                }
            }

            await using (var createCommand = new NpgsqlCommand(
                $"CREATE DATABASE {QuoteIdentifier(databaseName)}", connection))
            {
                await createCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(targetConnection)
                .Options;
            await using var dbContext = new AppDbContext(options);
            await dbContext.Database.MigrateAsync(cancellationToken);

            return E2EDatabaseResult.Success($"E2E database prepared: {databaseName}.");
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            return E2EDatabaseResult.Failure("Unable to prepare the E2E database. Verify PostgreSQL availability and database permissions.");
        }
    }

    public async Task<E2EDatabaseResult> CleanupAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        if (!TryGetConnections(databaseName, out var maintenanceConnection, out _, out var error))
        {
            return E2EDatabaseResult.Failure(error);
        }

        try
        {
            await using var connection = new NpgsqlConnection(maintenanceConnection);
            await connection.OpenAsync(cancellationToken);

            await using (var terminateCommand = new NpgsqlCommand(
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity " +
                "WHERE datname = @databaseName AND pid <> pg_backend_pid()", connection))
            {
                terminateCommand.Parameters.AddWithValue("databaseName", databaseName);
                await terminateCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var dropCommand = new NpgsqlCommand(
                $"DROP DATABASE IF EXISTS {QuoteIdentifier(databaseName)}", connection))
            {
                await dropCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            return E2EDatabaseResult.Success($"E2E database removed: {databaseName}.");
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            return E2EDatabaseResult.Failure("Unable to remove the E2E database. Verify PostgreSQL availability and database permissions.");
        }
    }

    private bool TryGetConnections(
        string databaseName,
        out string maintenanceConnection,
        out string targetConnection,
        out string error)
    {
        maintenanceConnection = string.Empty;
        targetConnection = string.Empty;

        if (!E2EDatabaseGuard.TryValidate(environment.EnvironmentName, databaseName, out error))
        {
            return false;
        }

        var configuredConnection = configuration["E2E:MaintenanceConnection"];
        if (string.IsNullOrWhiteSpace(configuredConnection))
        {
            error = "E2E:MaintenanceConnection is required.";
            return false;
        }

        try
        {
            var maintenanceBuilder = new NpgsqlConnectionStringBuilder(configuredConnection)
            {
                Database = "postgres"
            };
            var targetBuilder = new NpgsqlConnectionStringBuilder(maintenanceBuilder.ConnectionString)
            {
                Database = databaseName
            };

            maintenanceConnection = maintenanceBuilder.ConnectionString;
            targetConnection = targetBuilder.ConnectionString;
            error = string.Empty;
            return true;
        }
        catch (ArgumentException)
        {
            error = "The E2E maintenance connection is invalid.";
            return false;
        }
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";
}
