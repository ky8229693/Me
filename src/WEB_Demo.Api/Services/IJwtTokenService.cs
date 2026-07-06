using Microsoft.AspNetCore.Identity;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface IJwtTokenService
{
    AuthResponse CreateToken(IdentityUser user);
}
