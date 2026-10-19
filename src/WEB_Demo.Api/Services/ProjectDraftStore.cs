using System.Collections.Concurrent;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Services;

public interface IProjectDraftStore
{
    ProjectDraftResponse Add(string userId, ProjectDraftFields fields);
    bool TryTake(Guid id, string userId, out ProjectDraftFields? fields);
}

public sealed class InMemoryProjectDraftStore(TimeProvider timeProvider) : IProjectDraftStore
{
    private sealed record Entry(string UserId, ProjectDraftFields Fields, DateTimeOffset ExpiresAt);
    private readonly ConcurrentDictionary<Guid, Entry> _drafts = new();

    public ProjectDraftResponse Add(string userId, ProjectDraftFields fields)
    {
        var id = Guid.NewGuid();
        var expiresAt = timeProvider.GetUtcNow().AddMinutes(15);
        _drafts[id] = new Entry(userId, fields, expiresAt);
        return new ProjectDraftResponse(id, fields.Name, fields.Description, expiresAt);
    }

    public bool TryTake(Guid id, string userId, out ProjectDraftFields? fields)
    {
        fields = null;
        if (!_drafts.TryGetValue(id, out var entry) || entry.UserId != userId) return false;
        if (entry.ExpiresAt <= timeProvider.GetUtcNow()) { _drafts.TryRemove(id, out _); return false; }
        if (!_drafts.TryRemove(new KeyValuePair<Guid, Entry>(id, entry))) return false;
        fields = entry.Fields;
        return true;
    }
}
