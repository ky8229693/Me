using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class InMemoryJwtSessionRegistryTests
{
    [Fact]
    public void Register_TracksMultipleSessionsAndRequiresMatchingUser()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new InMemoryJwtSessionRegistry(cache);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);

        registry.Register("session-1", "user-1", expiresAt);
        registry.Register("session-2", "user-1", expiresAt);

        registry.IsActive("session-1", "user-1").Should().BeTrue();
        registry.IsActive("session-2", "user-1").Should().BeTrue();
        registry.IsActive("session-1", "user-2").Should().BeFalse();
        registry.IsActive("unknown", "user-1").Should().BeFalse();
    }

    [Fact]
    public void Revoke_RemovesOnlyTheSelectedSession()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new InMemoryJwtSessionRegistry(cache);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        registry.Register("session-1", "user-1", expiresAt);
        registry.Register("session-2", "user-1", expiresAt);

        registry.Revoke("session-1");

        registry.IsActive("session-1", "user-1").Should().BeFalse();
        registry.IsActive("session-2", "user-1").Should().BeTrue();
    }

    [Fact]
    public void RevokeAll_RemovesEverySessionForOnlyThatUser()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new InMemoryJwtSessionRegistry(cache);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        registry.Register("user-1-a", "user-1", expiresAt);
        registry.Register("user-1-b", "user-1", expiresAt);
        registry.Register("user-2-a", "user-2", expiresAt);

        registry.RevokeAll("user-1");

        registry.IsActive("user-1-a", "user-1").Should().BeFalse();
        registry.IsActive("user-1-b", "user-1").Should().BeFalse();
        registry.IsActive("user-2-a", "user-2").Should().BeTrue();
    }

    [Fact]
    public async Task ExpiredSession_IsInactiveAndCannotBeRevivedByLookup()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new InMemoryJwtSessionRegistry(cache);
        registry.Register("short-session", "user-1", DateTimeOffset.UtcNow.AddMilliseconds(25));

        await Task.Delay(75);

        registry.IsActive("short-session", "user-1").Should().BeFalse();
        registry.IsActive("short-session", "user-1").Should().BeFalse();
    }

    [Fact]
    public void CapacityEviction_FailsClosed()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1 });
        var registry = new InMemoryJwtSessionRegistry(cache);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        registry.Register("first", "user-1", expiresAt);

        registry.Register("second", "user-2", expiresAt);

        var activeCount = new[]
        {
            registry.IsActive("first", "user-1"),
            registry.IsActive("second", "user-2")
        }.Count(active => active);
        activeCount.Should().BeLessThanOrEqualTo(1);
    }

    [Fact]
    public void ConcurrentRegistrationAndRevocation_RemainsConsistent()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var registry = new InMemoryJwtSessionRegistry(cache);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(5);
        var sessionIds = Enumerable.Range(0, 100).Select(index => $"session-{index}").ToArray();

        Parallel.ForEach(sessionIds, jwtId => registry.Register(jwtId, "user-1", expiresAt));
        Parallel.ForEach(sessionIds.Where((_, index) => index % 2 == 0), registry.Revoke);

        sessionIds.Where((_, index) => index % 2 == 0)
            .Should().OnlyContain(jwtId => !registry.IsActive(jwtId, "user-1"));
        sessionIds.Where((_, index) => index % 2 != 0)
            .Should().OnlyContain(jwtId => registry.IsActive(jwtId, "user-1"));

        registry.RevokeAll("user-1");
        sessionIds.Should().OnlyContain(jwtId => !registry.IsActive(jwtId, "user-1"));
    }
}
