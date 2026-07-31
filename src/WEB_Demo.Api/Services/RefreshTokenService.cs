using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using WEB_Demo.Api.Data;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public enum RefreshRotationStatus
{
    Success,
    Invalid,
    Reused
}

public sealed record RefreshTokenIssue(string Token, string FamilyId, DateTimeOffset ExpiresAt);

public sealed record RefreshRotationResult(
    RefreshRotationStatus Status,
    string? Token = null,
    string? FamilyId = null,
    DateTimeOffset? ExpiresAt = null,
    IdentityUser? User = null);

public interface IRefreshTokenService
{
    Task<RefreshTokenIssue> IssueAsync(string userId);
    Task<RefreshRotationResult> RotateAsync(string? rawToken);
    Task<string?> RevokeByTokenAsync(string? rawToken, string reason);
    Task RevokeFamilyAsync(string familyId, string reason);
    Task RevokeUserAsync(string userId, string reason);
    Task<int> CleanupAsync(DateTimeOffset? now = null);
}

public sealed class RefreshTokenService(
    AppDbContext dbContext,
    UserManager<IdentityUser> userManager,
    IOptions<JwtOptions> options) : IRefreshTokenService
{
    private readonly JwtOptions _options = options.Value;

    public async Task<RefreshTokenIssue> IssueAsync(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        var rawToken = RefreshTokenCrypto.Generate();
        var record = new RefreshToken
        {
            TokenHash = RefreshTokenCrypto.Hash(rawToken),
            FamilyId = Guid.NewGuid().ToString("N"),
            UserId = userId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_options.RefreshTokenDays)
        };

        dbContext.RefreshTokens.Add(record);
        await dbContext.SaveChangesAsync();
        return new RefreshTokenIssue(rawToken, record.FamilyId, record.ExpiresAt);
    }

    public async Task<RefreshRotationResult> RotateAsync(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);
        }

        await using var transaction = await BeginTransactionAsync();
        var tokenHash = RefreshTokenCrypto.Hash(rawToken);
        var current = await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash);
        if (current is null)
        {
            return new RefreshRotationResult(RefreshRotationStatus.Invalid);
        }

        var now = DateTimeOffset.UtcNow;
        if (current.ConsumedAt is not null)
        {
            await RevokeFamilyRecordsAsync(current.FamilyId, now, "refresh-token-reuse");
            await CommitAsync(transaction);
            return new RefreshRotationResult(RefreshRotationStatus.Reused, FamilyId: current.FamilyId);
        }

        if (current.RevokedAt is not null || current.ExpiresAt <= now)
        {
            return new RefreshRotationResult(RefreshRotationStatus.Invalid, FamilyId: current.FamilyId);
        }

        var user = await userManager.FindByIdAsync(current.UserId);
        if (user is null || await userManager.IsLockedOutAsync(user))
        {
            await RevokeFamilyRecordsAsync(current.FamilyId, now, user is null ? "user-missing" : "user-disabled");
            await CommitAsync(transaction);
            return new RefreshRotationResult(RefreshRotationStatus.Invalid, FamilyId: current.FamilyId);
        }

        var replacementRaw = RefreshTokenCrypto.Generate();
        var replacement = new RefreshToken
        {
            TokenHash = RefreshTokenCrypto.Hash(replacementRaw),
            FamilyId = current.FamilyId,
            UserId = current.UserId,
            CreatedAt = now,
            ExpiresAt = current.ExpiresAt
        };
        current.ConsumedAt = now;
        current.ReplacementId = replacement.Id;
        current.ConcurrencyStamp = Guid.NewGuid();
        dbContext.RefreshTokens.Add(replacement);

        try
        {
            await dbContext.SaveChangesAsync();
            await CommitAsync(transaction);
        }
        catch (DbUpdateConcurrencyException)
        {
            await RollbackAsync(transaction);
            dbContext.ChangeTracker.Clear();
            await RevokeFamilyAsync(current.FamilyId, "concurrent-refresh-reuse");
            return new RefreshRotationResult(RefreshRotationStatus.Reused, FamilyId: current.FamilyId);
        }

        return new RefreshRotationResult(
            RefreshRotationStatus.Success,
            replacementRaw,
            current.FamilyId,
            current.ExpiresAt,
            user);
    }

    public async Task<string?> RevokeByTokenAsync(string? rawToken, string reason)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return null;
        var hash = RefreshTokenCrypto.Hash(rawToken);
        var token = await dbContext.RefreshTokens.AsNoTracking()
            .SingleOrDefaultAsync(item => item.TokenHash == hash);
        if (token is null) return null;
        await RevokeFamilyAsync(token.FamilyId, reason);
        return token.FamilyId;
    }

    public async Task RevokeFamilyAsync(string familyId, string reason)
    {
        await RevokeFamilyRecordsAsync(familyId, DateTimeOffset.UtcNow, reason);
    }

    public async Task RevokeUserAsync(string userId, string reason)
    {
        var now = DateTimeOffset.UtcNow;
        var records = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > now)
            .ToListAsync();
        foreach (var token in records)
        {
            token.RevokedAt = now;
            token.RevocationReason = reason;
            token.ConcurrencyStamp = Guid.NewGuid();
        }
        if (records.Count > 0) await dbContext.SaveChangesAsync();
    }

    public async Task<int> CleanupAsync(DateTimeOffset? now = null)
    {
        var cutoff = (now ?? DateTimeOffset.UtcNow).AddDays(-_options.RefreshTokenRetentionDays);
        var records = await dbContext.RefreshTokens
            .Where(token => token.ExpiresAt < cutoff || (token.RevokedAt != null && token.RevokedAt < cutoff))
            .ToListAsync();
        dbContext.RefreshTokens.RemoveRange(records);
        if (records.Count > 0) await dbContext.SaveChangesAsync();
        return records.Count;
    }

    private async Task RevokeFamilyRecordsAsync(string familyId, DateTimeOffset now, string reason)
    {
        var records = await dbContext.RefreshTokens
            .Where(token => token.FamilyId == familyId && token.RevokedAt == null)
            .ToListAsync();
        foreach (var token in records)
        {
            token.RevokedAt = now;
            token.RevocationReason = reason;
            token.ConcurrencyStamp = Guid.NewGuid();
        }
        if (records.Count > 0) await dbContext.SaveChangesAsync();
    }

    private async Task<IDbContextTransaction?> BeginTransactionAsync()
    {
        if (!dbContext.Database.IsRelational()) return null;
        return await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    }

    private static Task CommitAsync(IDbContextTransaction? transaction) =>
        transaction?.CommitAsync() ?? Task.CompletedTask;

    private static Task RollbackAsync(IDbContextTransaction? transaction) =>
        transaction?.RollbackAsync() ?? Task.CompletedTask;
}
