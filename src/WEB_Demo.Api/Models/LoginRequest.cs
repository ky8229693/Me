namespace WEB_Demo.Api.Models;

public sealed record LoginRequest(
    string Email,
    string Password);
