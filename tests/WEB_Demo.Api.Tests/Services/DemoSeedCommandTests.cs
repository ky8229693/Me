using FluentAssertions;
using WEB_Demo.Api.Bootstrap;

namespace WEB_Demo.Api.Tests.Services;

public sealed class DemoSeedCommandTests
{
    [Fact]
    public void TryParse_WithOwnerEmail_ReturnsCommand()
    {
        DemoSeedCommand.TryParse(["seed-demo-data", "--owner-email", "owner@example.com"], out var command, out var error)
            .Should().BeTrue();
        error.Should().BeEmpty();
        command!.OwnerEmail.Should().Be("owner@example.com");
    }

    [Theory]
    [InlineData("seed-demo-data")]
    [InlineData("seed-demo-data", "--unknown")]
    public void TryParse_WithInvalidArguments_ReturnsUsage(params string[] args)
    {
        DemoSeedCommand.TryParse(args, out var command, out var error).Should().BeFalse();
        command.Should().BeNull();
        error.Should().NotBeEmpty();
    }
}
