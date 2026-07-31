using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WEB_Demo.Api.Authorization;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    UserManager<IdentityUser> userManager,
    IPermissionResolver permissionResolver,
    IJwtSessionRegistry sessionRegistry) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<AuthResponse> CreateTokenAsync(IdentityUser user, string familyId)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var jwtId = Guid.NewGuid().ToString("N");
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, jwtId),
            new Claim("sid", familyId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var roles = await userManager.GetRolesAsync(user);
        var permissions = await permissionResolver.GetForRolesAsync(roles);
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(AppClaimTypes.Permission, permission)));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        sessionRegistry.Register(jwtId, user.Id, familyId, expiresAt);

        return new AuthResponse(
            accessToken,
            expiresAt,
            new AuthUser(user.Id, user.Email ?? string.Empty, roles.ToArray(), permissions));
    }
}
