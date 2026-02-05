using System.Net;
using System.Net.Http.Json;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using FluentAssertions;
using System.Text.Json;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
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

        var conn = db.Database.GetDbConnection();
        if (conn.State == System.Data.ConnectionState.Open) await conn.CloseAsync();
        await conn.OpenAsync();

        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task SuccessfulSignup_ReturnsOutcomeCode_AndNoHumanMessage()
    {
        var email = $"success_test_{Guid.NewGuid()}@example.com";
        var command = new SignUpCommand("Success Corp", "Test", "User", email, "SecurePass123!@#");

        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("outcomeCode").GetString().Should().Be("AUTH.SIGNUP.VERIFICATION_REQUIRED");
        
        // Zero-Text Policy: Message should be null
        result.GetProperty("message").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task SuccessfulLogin_ReturnsOutcomeCode_AndNoHumanMessage()
    {
        var email = $"login_test_{Guid.NewGuid()}@example.com";
        var signupCommand = new SignUpCommand("Login Corp", "Test", "User", email, "SecurePass123!@#");
        await _client.PostAsJsonAsync("/api/auth/signup", signupCommand);

        // Simulate activating user in DB (bypassing email verification for test)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.VerifyEmail();
                await db.SaveChangesAsync();
            }
        }

        var loginCommand = new LoginCommand(email, "SecurePass123!@#");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(json);

        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("outcomeCode").GetString().Should().Be("AUTH.LOGIN.COMPLETED");
        
        // Zero-Text Policy: Message should be null
        result.GetProperty("message").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
