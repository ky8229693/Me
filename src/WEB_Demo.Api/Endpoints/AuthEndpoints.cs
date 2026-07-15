using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth")
            .WithTags("Authentication");

        group.MapPost("/register", async (
            RegisterRequest request,
            UserManager<IdentityUser> userManager,
            IJwtTokenService tokenService) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return Results.BadRequest(
                    ApiResponse<AuthResponse>.Fail("Email and password are required.", ApiErrorCodes.ValidationError));
            }

            var email = request.Email.Trim();
            var user = new IdentityUser
            {
                UserName = email,
                Email = email
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return Results.BadRequest(
                    ApiResponse<AuthResponse>.Fail(
                        string.Join(" ", result.Errors.Select(error => error.Description)),
                        ApiErrorCodes.ValidationError));
            }

            await EnsureDefaultRoleAsync(user, userManager);

            return Results.Created(
                "/api/auth/me",
                ApiResponse<AuthResponse>.Ok(await tokenService.CreateTokenAsync(user), "Registered."));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IJwtTokenService tokenService) =>
        {
            var user = await userManager.FindByEmailAsync(request.Email.Trim());
            if (user is null)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.Fail("Invalid email or password.", ApiErrorCodes.Unauthorized),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
            if (!result.Succeeded)
            {
                return Results.Json(
                    ApiResponse<AuthResponse>.Fail("Invalid email or password.", ApiErrorCodes.Unauthorized),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            await EnsureDefaultRoleAsync(user, userManager);

            return Results.Ok(ApiResponse<AuthResponse>.Ok(await tokenService.CreateTokenAsync(user), "Logged in."));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<IdentityUser> userManager) =>
        {
            var userId = userManager.GetUserId(principal);
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            var roles = user is null ? [] : await userManager.GetRolesAsync(user);

            return Results.Ok(ApiResponse<AuthUser>.Ok(new AuthUser(userId ?? string.Empty, email, roles.ToArray())));
        }).RequireAuthorization();

        group.MapPost("/logout", (ClaimsPrincipal principal, IJwtSessionRegistry sessionRegistry) =>
        {
            var jwtId = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (!string.IsNullOrWhiteSpace(jwtId))
            {
                sessionRegistry.Revoke(jwtId);
            }

            return Results.Ok(ApiResponse<object>.Ok(null, "Logged out."));
        }).RequireAuthorization();

        return endpoints;
    }

    private static async Task EnsureDefaultRoleAsync(
        IdentityUser user,
        UserManager<IdentityUser> userManager)
    {
        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
        {
            return;
        }

        var result = await userManager.AddToRoleAsync(user, AppRoles.User);
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Unable to assign the default user role. {errors}");
        }
    }
}
