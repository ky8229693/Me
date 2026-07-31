namespace WEB_Demo.Api.Models;

public sealed record AdminUser(
    string Id,
    string Email,
    bool IsEnabled,
    IReadOnlyCollection<string> Roles);

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record UpdateAccountStatusRequest(bool IsEnabled);

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<string> Roles);
