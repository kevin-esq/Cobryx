using System.Net.Http.Json;
using Cobryx.IntegrationTests.Fakes;
using System.Net;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Common.Models;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Cobryx.IntegrationTests;

public class CookieTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CookieTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        _client.DefaultRequestHeaders.Add("User-Agent", "IntegrationTestClient/1.0");
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var roleRepo = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IRoleRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Cobryx.Domain.Interfaces.IUnitOfWork>();

        await db.Database.EnsureCreatedAsync();
        await Cobryx.Infrastructure.Persistence.DbInitializer.SeedRolesAsync(roleRepo, unitOfWork);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookie()
    {
        var email = $"test_{Guid.NewGuid()}@cobryx.com";
        var password = "P@ssword123!";

        await RegisterAndVerifyUser(email, password);

        var loginCmd = new LoginCommand(email, password, "IntegrationTestDevice");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCmd);
        var loginContent = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Login failed with {response.StatusCode}: {loginContent}");

        var cookies = response.Headers.GetValues("Set-Cookie").ToList();
        Assert.NotEmpty(cookies);
        Assert.Contains(cookies, c => c.Contains("refreshToken="));
        Assert.Contains(cookies, c => c.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookies, c => c.Contains("SameSite", StringComparison.OrdinalIgnoreCase));

        var content = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"refreshToken\":", content);
    }

    [Fact]
    public async Task RefreshToken_ValidCookie_ReturnsNewAccessToken()
    {
        var email = $"test_{Guid.NewGuid()}@cobryx.com";
        var password = "P@ssword123!";

        await RegisterAndVerifyUser(email, password);

        var loginCmd = new LoginCommand(email, password, "IntegrationTestDevice");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCmd);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var refreshResponse = await _client.PostAsJsonAsync("/api/auth/refresh-token", new { });

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var apiResponse = await refreshResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(JsonOptions);
        Assert.NotNull(apiResponse?.Data);
        Assert.NotNull(apiResponse.Data.Token);

        var cookies = refreshResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.NotEmpty(cookies);
        Assert.Contains(cookies, c => c.Contains("refreshToken="));
    }

    [Fact]
    public async Task Logout_ClearsRefreshTokenCookie()
    {
        var email = $"test_{Guid.NewGuid()}@cobryx.com";
        var password = "P@ssword123!";

        await RegisterAndVerifyUser(email, password);

        var loginCmd = new LoginCommand(email, password, "IntegrationTestDevice");
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCmd);
        var apiResponse = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>(JsonOptions);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiResponse!.Data!.Token);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;

        var cookies = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> RegisterAndVerifyUser(string email, string password)
    {
        var signupCmd = new SignUpCommand(
            "Test Business",
            "Test",
            "User",
            email,
            password
        );

        var response = await _client.PostAsJsonAsync("/api/auth/signup", signupCmd);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Signup failed with {response.StatusCode}: {content}");

        var emailService = _factory.Services.GetRequiredService<MockEmailService>();
        var token = emailService.GetLastToken(email);
        Assert.NotNull(token);

        var verifyCmd = new VerifyEmailCommand(token);
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyCmd);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        return email;
    }
}
