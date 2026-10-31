using FluentAssertions;
using WEB_Demo.Api.Models;
using WEB_Demo.Api.Services;

namespace WEB_Demo.Api.Tests.Services;

public sealed class ProjectDraftStoreTests
{
    [Fact]
    public void TryTake_IsUserBoundAndOneTime()
    {
        var store = new InMemoryProjectDraftStore(new MutableTimeProvider());
        var draft = store.Add("owner", new ProjectDraftFields("Portal", "Redesign"));

        store.TryTake(draft.Id, "other", out _).Should().BeFalse();
        store.TryTake(draft.Id, "owner", out var fields).Should().BeTrue();
        fields!.Name.Should().Be("Portal");
        store.TryTake(draft.Id, "owner", out _).Should().BeFalse();
    }

    [Fact]
    public void TryTake_RejectsExpiredDraft()
    {
        var clock = new MutableTimeProvider();
        var store = new InMemoryProjectDraftStore(clock);
        var draft = store.Add("owner", new ProjectDraftFields("Portal", null));

        clock.UtcNow = clock.UtcNow.AddMinutes(16);

        store.TryTake(draft.Id, "owner", out _).Should().BeFalse();
    }

    private sealed class MutableTimeProvider : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = new(2026, 8, 16, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }
}
