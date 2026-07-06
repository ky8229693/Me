using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

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

            return Results.Created(
                "/api/auth/me",
                ApiResponse<AuthResponse>.Ok(tokenService.CreateToken(user), "Registered."));
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

            return Results.Ok(ApiResponse<AuthResponse>.Ok(tokenService.CreateToken(user), "Logged in."));
        });

        group.MapGet("/me", (ClaimsPrincipal principal, UserManager<IdentityUser> userManager) =>
        {
            var userId = userManager.GetUserId(principal);
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

            return Results.Ok(ApiResponse<AuthUser>.Ok(new AuthUser(userId ?? string.Empty, email)));
        }).RequireAuthorization();

        group.MapPost("/logout", () =>
            Results.Ok(ApiResponse<object>.Ok(null, "Logged out.")));

        return endpoints;
    }
}
