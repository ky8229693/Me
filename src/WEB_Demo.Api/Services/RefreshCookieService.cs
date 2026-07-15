using Microsoft.Extensions.Options;
using WEB_Demo.Api.Options;

namespace WEB_Demo.Api.Services;

public interface IRefreshCookieService
{
    string? Read(HttpRequest request);
    void Write(HttpResponse response, string token, DateTimeOffset expiresAt);
    void Clear(HttpResponse response);
}

public sealed class RefreshCookieService(
    IOptions<JwtOptions> options,
    IWebHostEnvironment environment) : IRefreshCookieService
{
    private readonly JwtOptions _options = options.Value;

    public string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(_options.RefreshCookieName, out var token) ? token : null;

    public void Write(HttpResponse response, string token, DateTimeOffset expiresAt) =>
        response.Cookies.Append(_options.RefreshCookieName, token, CreateOptions(expiresAt));

    public void Clear(HttpResponse response) =>
        response.Cookies.Delete(_options.RefreshCookieName, CreateOptions(DateTimeOffset.UnixEpoch));

    private CookieOptions CreateOptions(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Strict,
        Path = "/api/auth",
        Expires = expiresAt,
        IsEssential = true
    };
}
