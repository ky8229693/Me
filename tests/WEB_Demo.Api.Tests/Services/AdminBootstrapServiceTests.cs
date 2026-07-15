using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Bootstrap;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class AdminBootstrapServiceTests
{
    [Fact]
    public async Task Bootstrap_NewAccount_CreatesAdminAndUserRoles()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"bootstrap-{Guid.NewGuid():N}@example.com";

        var result = await service.BootstrapAsync(email, "123456", false);
        var user = await userManager.FindByEmailAsync(email);

        result.Succeeded.Should().BeTrue();
        user.Should().NotBeNull();
        (await userManager.GetRolesAsync(user!)).Should().BeEquivalentTo([AppRoles.Admin, AppRoles.User]);
    }

    [Fact]
    public async Task Bootstrap_ExistingUserWithoutConfirmation_DoesNotPromote()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"existing-{Guid.NewGuid():N}@example.com";
        var user = new IdentityUser { UserName = email, Email = email };
        (await userManager.CreateAsync(user, "123456")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, AppRoles.User)).Succeeded.Should().BeTrue();

        var result = await service.BootstrapAsync(email, null, false);

        result.Succeeded.Should().BeFalse();
        (await userManager.IsInRoleAsync(user, AppRoles.Admin)).Should().BeFalse();
    }

    [Fact]
    public async Task Bootstrap_ExistingConfirmedUser_PromotesAccount()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"promote-{Guid.NewGuid():N}@example.com";
        var user = new IdentityUser { UserName = email, Email = email };
        (await userManager.CreateAsync(user, "123456")).Succeeded.Should().BeTrue();

        var result = await service.BootstrapAsync(email, null, true);

        result.Succeeded.Should().BeTrue();
        (await userManager.GetRolesAsync(user)).Should().BeEquivalentTo([AppRoles.Admin, AppRoles.User]);
    }

    [Fact]
    public async Task Bootstrap_ExistingConfirmedUser_RevokesExistingSessions()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var registry = scope.ServiceProvider.GetRequiredService<IJwtSessionRegistry>();
        var email = $"session-{Guid.NewGuid():N}@example.com";
        var user = new IdentityUser { UserName = email, Email = email };
        (await userManager.CreateAsync(user, "123456")).Succeeded.Should().BeTrue();
        registry.Register("existing-session", user.Id, DateTimeOffset.UtcNow.AddMinutes(5));

        var result = await service.BootstrapAsync(email, null, true);

        result.Succeeded.Should().BeTrue();
        registry.IsActive("existing-session", user.Id).Should().BeFalse();
    }

    [Fact]
    public async Task Bootstrap_WhenAdminExists_RejectsFurtherChanges()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();

        (await service.BootstrapAsync($"first-{Guid.NewGuid():N}@example.com", "123456", false)).Succeeded.Should().BeTrue();
        var result = await service.BootstrapAsync($"second-{Guid.NewGuid():N}@example.com", "123456", false);

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task Bootstrap_WhenTargetIsAlreadyAdmin_IsIdempotentEvenWithOtherAdmins()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var firstEmail = $"first-{Guid.NewGuid():N}@example.com";
        (await service.BootstrapAsync(firstEmail, "123456", false)).Succeeded.Should().BeTrue();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var secondEmail = $"second-{Guid.NewGuid():N}@example.com";
        var second = new IdentityUser { UserName = secondEmail, Email = secondEmail };
        (await userManager.CreateAsync(second, "123456")).Succeeded.Should().BeTrue();
        (await userManager.AddToRolesAsync(second, [AppRoles.User, AppRoles.Admin])).Succeeded.Should().BeTrue();

        var result = await service.BootstrapAsync(firstEmail, null, false);

        result.Succeeded.Should().BeTrue();
        result.Message.Should().Contain("already configured");
    }

    [Fact]
    public async Task Bootstrap_WithWeakPassword_DoesNotLeaveAccount()
    {
        await using var factory = CreateFactory();
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<AdminBootstrapService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var email = $"weak-{Guid.NewGuid():N}@example.com";

        var result = await service.BootstrapAsync(email, "1", false);

        result.Succeeded.Should().BeFalse();
        (await userManager.FindByEmailAsync(email)).Should().BeNull();
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var databaseName = $"bootstrap-tests-{Guid.NewGuid():N}";
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        });
    }
}
