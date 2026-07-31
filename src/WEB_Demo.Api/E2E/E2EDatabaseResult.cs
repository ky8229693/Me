namespace WEB_Demo.Api.E2E;

public sealed record E2EDatabaseResult(bool Succeeded, string Message)
{
    public static E2EDatabaseResult Success(string message) => new(true, message);
    public static E2EDatabaseResult Failure(string message) => new(false, message);
}
