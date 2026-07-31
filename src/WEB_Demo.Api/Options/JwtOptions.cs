namespace WEB_Demo.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 7;
    public int RefreshTokenRetentionDays { get; init; } = 30;
    public string RefreshCookieName { get; init; } = "web_demo.refresh_token";
}
