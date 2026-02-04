using System.Net.Http.Json;
using Cobryx.IntegrationTests.Fakes;
using System.Net;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Cobryx.IntegrationTests;

public class CookieTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CookieTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("User-Agent", "IntegrationTestClient/1.0");
    }

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
        Assert.Contains(cookies, c => c.Contains("HttpOnly"));
        Assert.Contains(cookies, c => c.Contains("SameSite=Strict"));

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

        var authResult = await refreshResponse.Content.ReadFromJsonAsync<AuthResult>();
        Assert.NotNull(authResult);
        Assert.NotNull(authResult.Token);

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
        var authResult = await loginResponse.Content.ReadFromJsonAsync<AuthResult>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResult.Token);

        var logoutResponse = await _client.PostAsJsonAsync("/api/auth/logout", new { });

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        _client.DefaultRequestHeaders.Authorization = null;

        var cookies = logoutResponse.Headers.GetValues("Set-Cookie").ToList();
        Assert.Contains(cookies, c => c.Contains("refreshToken=;") || c.Contains("refreshToken= ") || c.Contains("expires=Thu, 01 Jan 1970"));
    }

    private async Task<string> RegisterAndVerifyUser(string email, string password)
    {
        var registerCmd = new
        {
            BusinessName = "Test Business",
            FirstName = "Test",
            LastName = "User",
            Email = email,
            Password = password,
            TaxId = "XAXX010101000",
            Industry = "Tech",
            BusinessAddress = "123 Main St",
            MarketingConsent = true,
            TermsVersion = "v1.0",
            CaptchaToken = "dummy"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerCmd);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, $"Register failed with {response.StatusCode}: {content}");

        await Task.Delay(200);

        var emailService = _factory.Services.GetRequiredService<MockEmailService>();
        var body = emailService.LastBody;
        Assert.NotNull(body);

        var token = body.Split("token=")[1];

        var verifyCmd = new { Token = token };
        var verifyResponse = await _client.PostAsJsonAsync("/api/auth/verify-email", verifyCmd);
        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        return email;
    }
}
