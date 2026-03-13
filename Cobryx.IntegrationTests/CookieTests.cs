using System.Net;
using System.Net.Http.Json;

using Cobryx.Api.Contracts.V1.Identity;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Infrastructure.Persistence;
using Cobryx.IntegrationTests.Fakes;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Cobryx.IntegrationTests.Helpers;
using FluentAssertions;

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

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task Login_ValidCredentials_SetsRefreshTokenCookie()
    {
        var email = $"test_{Guid.NewGuid()}@cobryx.com";
        var password = "P@ssword123!";

        await RegisterAndVerifyUser(email, password);

        var loginCmd = new LoginCommand(email, password, "IntegrationTestDevice");
        var response = await _client.PostIdempotentAsync("/api/v1/auth/login", loginCmd);
        var loginContent = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, $"Login failed with {response.StatusCode}: {loginContent}");

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue("Login should set cookies");
        var cookieList = cookies!.ToList();
        cookieList.Should().Contain(c => c.Contains("refreshToken="));
        cookieList.Should().Contain(c => c.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase));
        cookieList.Should().Contain(c => c.Contains("SameSite", StringComparison.OrdinalIgnoreCase));

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
        var loginResponse = await _client.PostIdempotentAsync("/api/v1/auth/login", loginCmd);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponse = await _client.PostIdempotentAsync("/api/v1/auth/refresh-token", new { });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var apiResponse = await refreshResponse.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>(_jsonOptions);
        Assert.NotNull(apiResponse?.Data);
        Assert.NotNull(apiResponse.Data.AccessToken);

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
        var loginResponse = await _client.PostIdempotentAsync("/api/v1/auth/login", loginCmd);
        var apiResponse = await loginResponse.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>(_jsonOptions);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiResponse!.Data!.AccessToken);

        var logoutResponse = await _client.PostIdempotentAsync("/api/v1/auth/logout", new { });

        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _client.DefaultRequestHeaders.Authorization = null;

        logoutResponse.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(c => c.Contains("refreshToken=", StringComparison.OrdinalIgnoreCase));
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

        var response = await _client.PostIdempotentAsync("/api/v1/auth/signup", signupCmd);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Signup failed with {response.StatusCode}: {content}");

        var emailService = _factory.Services.GetRequiredService<MockEmailService>();
        var token = emailService.GetLastToken(email);
        Assert.NotNull(token);

        var verifyCmd = new VerifyEmailCommand(token);
        var verifyResponse = await _client.PostIdempotentAsync("/api/v1/auth/verify-email", verifyCmd);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return email;
    }
}
