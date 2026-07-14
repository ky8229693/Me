namespace WEB_Demo.Api.Bootstrap;

public sealed record AdminBootstrapResult(bool Succeeded, string Message)
{
    public static AdminBootstrapResult Success(string message) => new(true, message);
    public static AdminBootstrapResult Failure(string message) => new(false, message);
}
