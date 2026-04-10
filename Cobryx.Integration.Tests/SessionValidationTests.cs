using System.Net;
using System.Net.Http.Json;

using Cobryx.Api.Contracts.V1.Identity;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Integration.Tests.Fakes;
using Cobryx.Integration.Tests.Helpers;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Integration.Tests;

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
        if (conn.State == System.Data.ConnectionState.Open)
            await conn.CloseAsync();
        await conn.OpenAsync();

        await db.Database.EnsureCreatedAsync();
        await DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<string> RegisterAndLoginAsync(string email)
    {
        var signupResp = await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpRequest("Test", "User", "Test Corp", email, DefaultPassword));
        if (signupResp.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await signupResp.Content.ReadAsStringAsync();
            throw new Exception($"Signup failed with 400: {body}");
        }
        signupResp.EnsureSuccessStatusCode();

        var token = _emailService.GetLastToken(email);
        var verifyResp = await _client.PostAsJsonAsync("/api/v1/auth/verify-email", new Cobryx.Application.Auth.Commands.Core.VerifyEmailCommand(token!));
        verifyResp.EnsureSuccessStatusCode();

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        loginResp.EnsureSuccessStatusCode();

        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();
        loginResult.Should().NotBeNull();
        loginResult!.Data.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data!.AccessToken);
        return loginResult.Data.AccessToken!;
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

        var response = await _client.GetAsync("/api/v1/sessions");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

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

        var response = await _client.GetAsync("/api/v1/sessions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("errorCode").GetString().Should().Be("AUTH.NOT_AUTHENTICATED");
        root.TryGetProperty("message", out _).Should().BeFalse();
    }
}
