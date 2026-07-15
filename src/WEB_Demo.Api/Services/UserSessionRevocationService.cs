namespace WEB_Demo.Api.Services;

public interface IUserSessionRevocationService
{
    Task RevokeUserAsync(string userId, string reason);
}

public sealed class UserSessionRevocationService(
    IRefreshTokenService refreshTokens,
    IJwtSessionRegistry accessSessions) : IUserSessionRevocationService
{
    public async Task RevokeUserAsync(string userId, string reason)
    {
        await refreshTokens.RevokeUserAsync(userId, reason);
        accessSessions.RevokeAll(userId);
    }
}
