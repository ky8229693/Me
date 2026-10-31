using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using WEB_Demo.Api.Models;

namespace WEB_Demo.Api.Tests.Endpoints;

public sealed class ApiFoundationEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiFoundationEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_RedirectsToSwagger()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().Should().Be("/swagger");
    }

    [Fact]
    public async Task Health_ReturnsSuccessfulApiResponse()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<HealthResponse>>();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.ErrorCode.Should().BeNull();
        body.Data.Should().NotBeNull();
        body.Data!.Status.Should().Be("Healthy");
        body.Data.Service.Should().Be("WEB_Demo.Api");
    }

    [Fact]
    public async Task RemovedTodoRoute_ReturnsNotFound()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/todos");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record HealthResponse(
        string Status,
        string Service,
        DateTimeOffset CheckedAt,
        double Duration);
}
