namespace WEB_Demo.Api.Services;

public interface IJwtSessionRegistry
{
    void Register(string jwtId, string userId, DateTimeOffset expiresAt);
    void Register(string jwtId, string userId, string familyId, DateTimeOffset expiresAt);

    bool IsActive(string jwtId, string userId);
    bool IsActive(string jwtId, string userId, string familyId);

    void Revoke(string jwtId);

    void RevokeAll(string userId);
    void RevokeFamily(string familyId);
}
