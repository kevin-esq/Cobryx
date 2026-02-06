using System.Net;
using System.Net.Http.Json;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Cobryx.IntegrationTests.Fakes;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Cobryx.Domain.Enums;

namespace Cobryx.IntegrationTests;

[Collection("Sequential")]
public class SessionValidationTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MockEmailService _emailService;
    private const string DefaultPassword = "Password123!@#";

    public SessionValidationTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        _emailService = factory.Services.GetRequiredService<MockEmailService>();
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

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Test Corp", "Test", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new Cobryx.Application.Auth.Commands.Core.VerifyEmailCommand(token!));
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiSuccessResponse<AuthResult>>();
        loginResult.Should().NotBeNull();
        loginResult!.Data.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data!.Token);
        return loginResult.Data.Token!;
    }

    [Fact]
    public async Task Access_WithLockedAccount_ReturnsProblemDetails()
    {
        var email = $"locked_{Guid.NewGuid():N}@example.com";
        await RegisterAndLoginAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.FirstAsync(u => u.Email == email);
            user.Lock();
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/auth/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden); // Or 401 depending on mapping

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("success").GetBoolean().Should().BeFalse();
        root.TryGetProperty("message", out _).Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be("AUTH.ACCOUNT_LOCKED");
    }

    [Fact]
    public async Task Access_WithRevokedSession_ReturnsProblemDetails()
    {
        var email = $"revoked_{Guid.NewGuid():N}@example.com";
        await RegisterAndLoginAsync(email);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.Include(u => u.Sessions).FirstAsync(u => u.Email == email);
            user.Sessions.First().Revoke();
            await db.SaveChangesAsync();
        }

        var response = await _client.GetAsync("/api/auth/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("errorCode").GetString().Should().Be("AUTH.NOT_AUTHENTICATED");
        root.TryGetProperty("message", out _).Should().BeFalse();
    }
}
