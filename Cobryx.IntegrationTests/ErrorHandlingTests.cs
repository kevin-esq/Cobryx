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
        if (conn.State == System.Data.ConnectionState.Open) await conn.CloseAsync();
        await conn.OpenAsync();

        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task ValidationFailure_ReturnsProblemDetails_WithCodeAndErrors()
    {
        var command = new SignUpCommand("", "", "", "invalid-email", "short");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<JsonElement>(json);

        problemDetails.GetProperty("success").GetBoolean().Should().BeFalse();
        problemDetails.TryGetProperty("message", out _).Should().BeFalse();
        problemDetails.GetProperty("errorCode").GetString().Should().Be("VALIDATION.FAILED");
        problemDetails.GetProperty("numericCode").GetInt32().Should().Be(1001);

        Console.WriteLine($"DEBUG: FULL JSON: {json}");

        var errors = problemDetails.GetProperty("errors");
        errors.ValueKind.Should().Be(JsonValueKind.Array);

        var businessNameError = errors.EnumerateArray().FirstOrDefault(e =>
            e.TryGetProperty("field", out var f) && (f.GetString()?.Equals("businessName", StringComparison.OrdinalIgnoreCase) == true || f.GetString()?.Equals("BusinessName", StringComparison.OrdinalIgnoreCase) == true));
        businessNameError.ValueKind.Should().NotBe(JsonValueKind.Undefined);
        businessNameError.GetProperty("code").GetString().Should().Be("VALIDATION.AUTH.BUSINESS_NAME.REQUIRED");

        var emailError = errors.EnumerateArray().FirstOrDefault(e =>
            e.TryGetProperty("field", out var f) && (f.GetString()?.Equals("email", StringComparison.OrdinalIgnoreCase) == true || f.GetString()?.Equals("Email", StringComparison.OrdinalIgnoreCase) == true));
        emailError.GetProperty("code").GetString().Should().Be("VALIDATION.AUTH.EMAIL.INVALID");
    }

    [Fact]
    public async Task NoHumanReadableText_InValidationErrors()
    {
        var command = new SignUpCommand("", "", "", "invalid", "short");
        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", command);

        var json = await response.Content.ReadAsStringAsync();

        json.Should().NotContain("required");
        json.Should().NotContain("must not be empty");
        json.Should().NotContain("format");
        json.Should().NotContain("longer than");
    }

    [Fact]
    public async Task DomainError_InvalidCredentials_ReturnsProblemDetails_WithAuthCode()
    {
        var email = $"existing_error_test_{Guid.NewGuid()}@example.com";
        var signupResponse = await _client.PostAsJsonAsync("/api/v1/auth/signup", new SignUpCommand("Test Corp", "Test", "User", email, "SecurePass123!@#"));
        signupResponse.EnsureSuccessStatusCode();

        var command = new LoginCommand(email, "WrongPassword!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.TryGetProperty("message", out _).Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
        root.GetProperty("numericCode").GetInt32().Should().Be(1101);
        root.GetProperty("traceId").GetString().Should().NotBeNullOrEmpty();
    }
}
