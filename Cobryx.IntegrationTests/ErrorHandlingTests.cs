using System.Net;
using System.Net.Http.Json;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using System.Text.Json;

using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.IntegrationTests;

public class ErrorHandlingTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ErrorHandlingTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var conn = db.Database.GetDbConnection();
        // Reset connection to ensure fresh DB state if reused
        if (conn.State == System.Data.ConnectionState.Open) await conn.CloseAsync();
        await conn.OpenAsync();

        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ValidationFailure_ReturnsProblemDetails_WithCodeAndErrors()
    {
        // Arrange
        var command = new SignUpCommand("", "", "", "invalid-email", "short"); // Invalid data

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(json);

        problemDetails.GetProperty("status").GetInt32().Should().Be(400);
        problemDetails.GetProperty("title").GetString().Should().Be("Validation Failed");
        problemDetails.GetProperty("code").GetString().Should().Be("VALIDATION.FAILED");
        problemDetails.GetProperty("numericCode").GetInt32().Should().Be(1001);

        var errors = problemDetails.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Object);
        errors.TryGetProperty("BusinessName", out _).Should().BeTrue();
        errors.TryGetProperty("Email", out _).Should().BeTrue();
    }

    [Fact]
    public async Task DomainError_InvalidCredentials_ReturnsProblemDetails_WithAuthCode()
    {
        // Arrange
        // Create a user first to avoid SQLite in-memory specific crash when querying non-existent user with includes
        var email = $"existing_error_test_{Guid.NewGuid()}@example.com";
        var signupResponse = await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Test Corp", "Test", "User", email, "SecurePass123!@#"));
        signupResponse.EnsureSuccessStatusCode();

        var command = new LoginCommand(email, "WrongPassword!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("status").GetInt32().Should().Be(401);
        root.GetProperty("title").GetString().Should().Be("Invalid Credentials");
        root.GetProperty("code").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
        root.GetProperty("numericCode").GetInt32().Should().Be(1101); // Numeric Code Verification
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }
}
