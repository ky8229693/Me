namespace WEB_Demo.Api.Services;

public enum AdministrationResultStatus
{
    Success,
    ValidationError,
    NotFound,
    Forbidden,
    Conflict
}

public sealed record AdministrationResult<T>(
    AdministrationResultStatus Status,
    T? Data,
    string Message)
{
    public static AdministrationResult<T> Success(T data, string message = "OK") =>
        new(AdministrationResultStatus.Success, data, message);

    public static AdministrationResult<T> Validation(string message) =>
        new(AdministrationResultStatus.ValidationError, default, message);

    public static AdministrationResult<T> NotFound(string message) =>
        new(AdministrationResultStatus.NotFound, default, message);

    public static AdministrationResult<T> Forbidden(string message) =>
        new(AdministrationResultStatus.Forbidden, default, message);

    public static AdministrationResult<T> Conflict(string message) =>
        new(AdministrationResultStatus.Conflict, default, message);
}
