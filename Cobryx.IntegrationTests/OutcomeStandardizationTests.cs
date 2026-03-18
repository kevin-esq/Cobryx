using System.Net;
using System.Text.Json;

using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Cobryx.IntegrationTests.Helpers;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.IntegrationTests;

public class OutcomeStandardizationTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OutcomeStandardizationTests(CobryxWebApplicationFactory factory)
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

        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SuccessfulSignup_ReturnsOutcomeCode_AndNoHumanMessage()
    {
        var email = $"success_test_{Guid.NewGuid()}@example.com";
        var command = new SignUpCommand("Success Corp", "Test", "User", email, "SecurePass123!@#");

        var response = await _client.PostIdempotentAsync("/api/v1/auth/signup", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("outcomeCode").GetString().Should().Be("AUTH.USER.VERIFICATION_REQUIRED");
    }

    [Fact]
    public async Task SuccessfulLogin_ReturnsOutcomeCode_AndNoHumanMessage()
    {
        var email = $"login_test_{Guid.NewGuid()}@example.com";
        var signupCommand = new SignUpCommand("Login Corp", "Test", "User", email, "SecurePass123!@#");
        await _client.PostIdempotentAsync("/api/v1/auth/signup", signupCommand);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == (Cobryx.Domain.ValueObjects.EmailAddress)email);
            user.Should().NotBeNull("User should exist after signup");
            user!.VerifyEmail();
            await db.SaveChangesAsync();
        }

        var loginCommand = new LoginCommand(email, "SecurePass123!@#");
        var response = await _client.PostIdempotentAsync("/api/v1/auth/login", loginCommand);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("outcomeCode").GetString().Should().Be("AUTH.USER.LOGIN_SUCCESS");
    }
}
