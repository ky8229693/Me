using Microsoft.Extensions.Caching.Memory;

namespace WEB_Demo.Api.Services;

public sealed class InMemoryJwtSessionRegistry(IMemoryCache cache) : IJwtSessionRegistry
{
    private const string CacheKeyPrefix = "jwt-session:";
    private readonly object _indexLock = new();
    private readonly Dictionary<string, HashSet<string>> _sessionsByUser =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _sessionsByFamily =
        new(StringComparer.Ordinal);

    public void Register(string jwtId, string userId, DateTimeOffset expiresAt) =>
        Register(jwtId, userId, jwtId, expiresAt);

    public void Register(string jwtId, string userId, string familyId, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jwtId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(familyId);

        var entry = new JwtSessionEntry(jwtId, userId, familyId, expiresAt);
        lock (_indexLock)
        {
            AddToIndex(_sessionsByUser, userId, jwtId);
            AddToIndex(_sessionsByFamily, familyId, jwtId);
        }

        cache.Set(
            GetCacheKey(jwtId),
            entry,
            new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(expiresAt)
                .SetSize(1)
                .RegisterPostEvictionCallback(
                    static (_, value, _, state) =>
                    {
                        if (value is JwtSessionEntry removed && state is InMemoryJwtSessionRegistry registry)
                        {
                            registry.RemoveFromIndex(removed);
                        }
                    },
                    this));
    }

    public bool IsActive(string jwtId, string userId) =>
        !string.IsNullOrWhiteSpace(jwtId) &&
        !string.IsNullOrWhiteSpace(userId) &&
        cache.TryGetValue<JwtSessionEntry>(GetCacheKey(jwtId), out var entry) &&
        entry is not null &&
        entry.ExpiresAt > DateTimeOffset.UtcNow &&
        string.Equals(entry.UserId, userId, StringComparison.Ordinal);

    public bool IsActive(string jwtId, string userId, string familyId) =>
        !string.IsNullOrWhiteSpace(familyId) &&
        cache.TryGetValue<JwtSessionEntry>(GetCacheKey(jwtId), out var entry) &&
        entry is not null &&
        entry.ExpiresAt > DateTimeOffset.UtcNow &&
        string.Equals(entry.UserId, userId, StringComparison.Ordinal) &&
        string.Equals(entry.FamilyId, familyId, StringComparison.Ordinal);

    public void Revoke(string jwtId)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
        {
            return;
        }

        if (cache.TryGetValue<JwtSessionEntry>(GetCacheKey(jwtId), out var entry) && entry is not null)
        {
            RemoveFromIndex(entry);
        }

        cache.Remove(GetCacheKey(jwtId));
    }

    public void RevokeAll(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        string[] jwtIds;
        lock (_indexLock)
        {
            if (!_sessionsByUser.Remove(userId, out var sessions))
            {
                return;
            }

            jwtIds = sessions.ToArray();
        }

        foreach (var jwtId in jwtIds)
        {
            cache.Remove(GetCacheKey(jwtId));
        }
    }

    public void RevokeFamily(string familyId)
    {
        if (string.IsNullOrWhiteSpace(familyId)) return;

        string[] jwtIds;
        lock (_indexLock)
        {
            if (!_sessionsByFamily.Remove(familyId, out var sessions)) return;
            jwtIds = sessions.ToArray();
        }

        foreach (var jwtId in jwtIds) cache.Remove(GetCacheKey(jwtId));
    }

    private void RemoveFromIndex(JwtSessionEntry entry)
    {
        lock (_indexLock)
        {
            if (_sessionsByUser.TryGetValue(entry.UserId, out var sessions))
            {
                sessions.Remove(entry.JwtId);
                if (sessions.Count == 0)
                {
                    _sessionsByUser.Remove(entry.UserId);
                }
            }

            if (_sessionsByFamily.TryGetValue(entry.FamilyId, out var familySessions))
            {
                familySessions.Remove(entry.JwtId);
                if (familySessions.Count == 0) _sessionsByFamily.Remove(entry.FamilyId);
            }
        }
    }

    private static void AddToIndex(
        Dictionary<string, HashSet<string>> index,
        string key,
        string jwtId)
    {
        if (!index.TryGetValue(key, out var sessions))
        {
            sessions = new HashSet<string>(StringComparer.Ordinal);
            index[key] = sessions;
        }
        sessions.Add(jwtId);
    }

    private static string GetCacheKey(string jwtId) => $"{CacheKeyPrefix}{jwtId}";

    private sealed record JwtSessionEntry(string JwtId, string UserId, string FamilyId, DateTimeOffset ExpiresAt);
}
