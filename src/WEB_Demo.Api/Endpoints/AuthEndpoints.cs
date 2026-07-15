using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;
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
            IJwtTokenService tokenService,
            IRefreshTokenService refreshTokens,
            IRefreshCookieService refreshCookie,
            HttpResponse response) =>
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
            var refresh = await refreshTokens.IssueAsync(user.Id);
            refreshCookie.Write(response, refresh.Token, refresh.ExpiresAt);

            return Results.Created(
                "/api/auth/me",
                ApiResponse<AuthResponse>.Ok(await tokenService.CreateTokenAsync(user, refresh.FamilyId), "Registered."));
        });

        group.MapPost("/login", async (
            LoginRequest request,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IJwtTokenService tokenService,
            IRefreshTokenService refreshTokens,
            IRefreshCookieService refreshCookie,
            HttpResponse response) =>
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
            var refresh = await refreshTokens.IssueAsync(user.Id);
            refreshCookie.Write(response, refresh.Token, refresh.ExpiresAt);

            return Results.Ok(ApiResponse<AuthResponse>.Ok(
                await tokenService.CreateTokenAsync(user, refresh.FamilyId), "Logged in."));
        });

        group.MapPost("/refresh", async (
            HttpRequest request,
            HttpResponse response,
            IOptions<ApiCorsOptions> corsOptions,
            IRefreshCookieService refreshCookie,
            IRefreshTokenService refreshTokens,
            IJwtSessionRegistry sessionRegistry,
            IJwtTokenService tokenService) =>
        {
            if (!IsAllowedOrigin(request, corsOptions.Value))
            {
                refreshCookie.Clear(response);
                return Results.Json(
                    ApiResponse<AuthResponse>.Fail("The request origin is not allowed.", ApiErrorCodes.Forbidden),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var result = await refreshTokens.RotateAsync(refreshCookie.Read(request));
            if (result.Status != RefreshRotationStatus.Success ||
                result.User is null || result.Token is null || result.FamilyId is null || result.ExpiresAt is null)
            {
                if (!string.IsNullOrWhiteSpace(result.FamilyId)) sessionRegistry.RevokeFamily(result.FamilyId);
                refreshCookie.Clear(response);
                return Results.Json(
                    ApiResponse<AuthResponse>.Fail("The refresh session is no longer valid.", ApiErrorCodes.Unauthorized),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            sessionRegistry.RevokeFamily(result.FamilyId);
            var auth = await tokenService.CreateTokenAsync(result.User, result.FamilyId);
            refreshCookie.Write(response, result.Token, result.ExpiresAt.Value);
            return Results.Ok(ApiResponse<AuthResponse>.Ok(auth, "Session refreshed."));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, UserManager<IdentityUser> userManager) =>
        {
            var userId = userManager.GetUserId(principal);
            var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
            var user = userId is null ? null : await userManager.FindByIdAsync(userId);
            var roles = user is null ? [] : await userManager.GetRolesAsync(user);

            return Results.Ok(ApiResponse<AuthUser>.Ok(new AuthUser(userId ?? string.Empty, email, roles.ToArray())));
        }).RequireAuthorization();

        group.MapPost("/logout", async (
            HttpRequest request,
            HttpResponse response,
            IRefreshCookieService refreshCookie,
            IRefreshTokenService refreshTokens,
            IJwtSessionRegistry sessionRegistry) =>
        {
            var familyId = await refreshTokens.RevokeByTokenAsync(refreshCookie.Read(request), "logout");
            if (!string.IsNullOrWhiteSpace(familyId)) sessionRegistry.RevokeFamily(familyId);
            refreshCookie.Clear(response);

            return Results.Ok(ApiResponse<object>.Ok(null, "Logged out."));
        });

        return endpoints;
    }

    private static bool IsAllowedOrigin(HttpRequest request, ApiCorsOptions options)
    {
        var origin = request.Headers.Origin.ToString();
        return string.IsNullOrWhiteSpace(origin) ||
            options.AllowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
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
