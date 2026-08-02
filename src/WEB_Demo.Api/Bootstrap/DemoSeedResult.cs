namespace WEB_Demo.Api.Bootstrap;

public sealed record DemoSeedResult(bool Succeeded, string Message)
{
    public static DemoSeedResult Success(string message) => new(true, message);
    public static DemoSeedResult Failure(string message) => new(false, message);
}
