using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface IJwtTokenService
{
    Task<AuthResponse> CreateTokenAsync(IdentityUser user, string familyId);
}
