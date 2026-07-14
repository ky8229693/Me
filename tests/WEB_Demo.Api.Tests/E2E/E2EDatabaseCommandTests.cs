using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WEB_Demo.Api.E2E;

namespace WEB_Demo.Api.Tests.E2E;

public sealed class E2EDatabaseCommandTests
{
    [Fact]
    public void TryParse_WithPrepareArguments_ReturnsCommand()
    {
        var parsed = E2EDatabaseCommand.TryParse(
            ["e2e-database", "prepare", "--database", "web_demo_e2e_20260714_abcd1234", "--admin-email", "admin@example.test", "--password-env", "E2E_PASSWORD"],
            out var command,
            out _);

        parsed.Should().BeTrue();
        command.Should().NotBeNull();
        command!.Action.Should().Be("prepare");
        command.DatabaseName.Should().Be("web_demo_e2e_20260714_abcd1234");
    }

    [Theory]
    [InlineData("Development", "web_demo_e2e_20260714_abcd1234")]
    [InlineData("Production", "web_demo_e2e_20260714_abcd1234")]
    [InlineData("E2E", "web_demo")]
    [InlineData("E2E", "postgres")]
    [InlineData("E2E", "customer_database")]
    public void Guard_RejectsUnsafeTargets(string environment, string database)
    {
        E2EDatabaseGuard.TryValidate(environment, database, out _).Should().BeFalse();
    }

    [Fact]
    public void Guard_AllowsUniqueE2ETarget()
    {
        E2EDatabaseGuard.TryValidate("E2E", "web_demo_e2e_20260714_abcd1234", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CleanupAsync_WithDevelopmentDatabase_RejectsBeforeConnecting()
    {
        var service = CreateService("E2E", "Host=invalid;Database=postgres;Username=user;Password=secret;Timeout=1");

        var result = await service.CleanupAsync("web_demo");

        result.Succeeded.Should().BeFalse();
        result.Message.Should().NotContain("secret");
    }

    [Fact]
    public async Task PrepareAsync_WhenPostgresIsUnavailable_ReturnsSafeFailure()
    {
        var service = CreateService("E2E", "Host=127.0.0.1;Port=1;Database=postgres;Username=user;Password=secret;Timeout=1");

        var result = await service.PrepareAsync("web_demo_e2e_20260714_failure1");

        result.Succeeded.Should().BeFalse();
        result.Message.Should().NotContain("secret");
    }

    private static E2EDatabaseLifecycleService CreateService(string environment, string connection)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["E2E:MaintenanceConnection"] = connection
            })
            .Build();
        return new E2EDatabaseLifecycleService(configuration, new TestHostEnvironment(environment));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WEB_Demo.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
