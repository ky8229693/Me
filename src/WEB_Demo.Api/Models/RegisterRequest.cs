namespace WEB_Demo.Api.Models;

public sealed record RegisterRequest(
    string Email,
    string Password);
