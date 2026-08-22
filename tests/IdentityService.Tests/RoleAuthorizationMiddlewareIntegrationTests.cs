using System.Net;
using System.Security.Claims;
using IdentityService.Authorization;
using IdentityService.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityService.Tests;

public class RoleAuthorizationMiddlewareIntegrationTests
{
    [Fact]
    public async Task AuthorizedAdminRole_PassesThroughNormally()
    {
        await using var app =
            await CreateTestApplicationAsync("ADMIN");

        using var client = app.GetTestClient();

        var response =
            await client.GetAsync("/protected/admin");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "Protected endpoint reached.",
            await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnauthorizedStudentRole_ReturnsForbidden()
    {
        await using var app =
            await CreateTestApplicationAsync("STUDENT");

        using var client = app.GetTestClient();

        var response =
            await client.GetAsync("/protected/admin");

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ReturnsUnauthorized()
    {
        await using var app =
            await CreateTestApplicationAsync(null);

        using var client = app.GetTestClient();

        var response =
            await client.GetAsync("/protected/admin");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PublicEndpoint_PassesWithoutAuthentication()
    {
        await using var app =
            await CreateTestApplicationAsync(null);

        using var client = app.GetTestClient();

        var response =
            await client.GetAsync("/public");

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Equal(
            "Public endpoint reached.",
            await response.Content.ReadAsStringAsync());
    }

    private static async Task<WebApplication>
        CreateTestApplicationAsync(string? role)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();
        builder.Services.AddRouting();

        var app = builder.Build();

        app.UseRouting();

        // Creates an authenticated user only for integration tests.
        app.Use(async (context, next) =>
        {
            if (!string.IsNullOrWhiteSpace(role))
            {
                var claims = new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        "test-user"),

                    new Claim(
                        ClaimTypes.Role,
                        role)
                };

                var identity = new ClaimsIdentity(
                    claims,
                    authenticationType: "TestAuthentication");

                context.User =
                    new ClaimsPrincipal(identity);
            }

            await next();
        });

        app.UseMiddleware<RoleAuthorizationMiddleware>();

        app.MapGet(
                "/protected/admin",
                () => "Protected endpoint reached.")
            .WithMetadata(
                new RequireRoleAttribute("ADMIN"));

        app.MapGet(
            "/public",
            () => "Public endpoint reached.");

        await app.StartAsync();

        return app;
    }
}