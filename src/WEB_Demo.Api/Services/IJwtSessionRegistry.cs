namespace WEB_Demo.Api.Services;

public interface IJwtSessionRegistry
{
    void Register(string jwtId, string userId, DateTimeOffset expiresAt);

    bool IsActive(string jwtId, string userId);

    void Revoke(string jwtId);

    void RevokeAll(string userId);
}
