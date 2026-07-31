using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace WEB_Demo.Api.Authorization;

public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (AppPermissions.IsKnown(requirement.Permission) && context.User.HasClaim(AppClaimTypes.Permission, requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "Permission:";
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal)) return base.GetPolicyAsync(policyName);
        var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[Prefix.Length..])).Build();
        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
    public static string For(string permission) => $"{Prefix}{permission}";
}
