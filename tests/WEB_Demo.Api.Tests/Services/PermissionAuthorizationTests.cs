using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using WEB_Demo.Api.Authorization;

namespace WEB_Demo.Api.Tests.Services;

public sealed class PermissionAuthorizationTests
{
    [Fact]
    public void Catalog_HasUniqueValidKeys()
    {
        var act = AppPermissions.Validate;
        act.Should().NotThrow();
        AppPermissions.Catalog.Select(item => item.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Handler_WithKnownClaim_Succeeds()
    {
        var requirement = new PermissionRequirement(AppPermissions.RolesRead);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AppClaimTypes.Permission, AppPermissions.RolesRead)], "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_WithUnknownPermission_FailsClosed()
    {
        var requirement = new PermissionRequirement("unknown.manage");
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(AppClaimTypes.Permission, "unknown.manage")], "test"));
        var context = new AuthorizationHandlerContext([requirement], principal, null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
