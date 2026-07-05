namespace WEB_Demo.Api.Models;

public sealed record ApiResponse<T>(
    bool Success,
    string? Message,
    T? Data,
    string? ErrorCode = null)
{
    public static ApiResponse<T> Ok(T? data, string? message = "OK")
    {
        return new ApiResponse<T>(true, message, data);
    }

    public static ApiResponse<T> Fail(string message, string errorCode)
    {
        return new ApiResponse<T>(false, message, default, errorCode);
    }
}
