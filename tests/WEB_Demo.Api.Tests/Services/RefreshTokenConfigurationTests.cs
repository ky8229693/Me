using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Options;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class RefreshTokenConfigurationTests
{
    [Fact]
    public void JwtOptions_UseShortAccessAndFixedRefreshDefaults()
    {
        var options = new JwtOptions();

        options.AccessTokenMinutes.Should().Be(15);
        options.RefreshTokenDays.Should().Be(7);
        options.RefreshCookieName.Should().Be("web_demo.refresh_token");
    }

    [Theory]
    [InlineData("Development", false)]
    [InlineData("Production", true)]
    public void RefreshCookie_UsesEnvironmentSecurePolicy(string environmentName, bool expectedSecure)
    {
        var service = new RefreshCookieService(
            Microsoft.Extensions.Options.Options.Create(new JwtOptions()),
            new TestEnvironment { EnvironmentName = environmentName });
        var context = new DefaultHttpContext();

        service.Write(context.Response, "raw-token", DateTimeOffset.UtcNow.AddDays(7));

        var header = context.Response.Headers.SetCookie.Single()!.ToLowerInvariant();
        header.Should().Contain("httponly");
        header.Should().Contain("samesite=strict");
        header.Should().Contain("path=/api/auth");
        if (expectedSecure) header.Should().Contain("secure");
        else header.Should().NotContain("secure");
    }

    [Fact]
    public void RefreshTokenModel_DefinesRequiredLookupIndexes()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"refresh-model-{Guid.NewGuid():N}")
            .Options;
        using var context = new AppDbContext(options);
        var entity = context.Model.FindEntityType(typeof(RefreshToken));

        entity.Should().NotBeNull();
        entity!.GetIndexes().Single(index => index.Properties.Single().Name == nameof(RefreshToken.TokenHash))
            .IsUnique.Should().BeTrue();
        entity.GetIndexes().Select(index => index.Properties.Single().Name).Should()
            .Contain([nameof(RefreshToken.FamilyId), nameof(RefreshToken.UserId), nameof(RefreshToken.ExpiresAt)]);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "WEB_Demo.Api.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
