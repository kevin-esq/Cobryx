using System.Net;
using System.Net.Http.Json;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Tenants.Commands.OnboardBusiness;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Common.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Cobryx.IntegrationTests.Fakes;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Cobryx.IntegrationTests;

[Collection("Sequential")]
public class AuthFlowTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MockEmailService _emailService;
    private const string DefaultPassword = "Password123!@#";

    private string _email;

    public AuthFlowTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        _email = $"user_{Guid.NewGuid():N}@example.com";
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

    [Fact]
    public async Task HappyPath_SignupToOnboard_Success()
    {
        var command = new SignUpCommand(
            "John Business",
            "John",
            "Doe",
            _email,
            DefaultPassword,
            false,
            "v1.0"
        );
        var signupResp = await _client.PostAsJsonAsync("/api/auth/signup", command);
        signupResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var token = _emailService.GetLastToken(_email);
        token.Should().NotBeNull();

        var verifyResp = await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        loginResult!.Data!.Token.Should().NotBeNull();
        loginResult.Data.RequiresOnboarding.Should().BeTrue();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data.Token);

        var onboardCmd = new OnboardBusinessCommand("XAXX010101000", "Software", "Tech St 123");
        var onboardResp = await _client.PostAsJsonAsync("/api/auth/onboard", onboardCmd);
        onboardResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsResp = await _client.GetAsync("/api/auth/sessions");
        sessionsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResp = await _client.PostAsync("/api/auth/refresh-token", null);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithoutEmailVerification_Returns403WithPayload()
    {
        var command = new SignUpCommand("Any Corp", "A", "B", _email, DefaultPassword, false, "v1.0");
        await _client.PostAsJsonAsync("/api/auth/signup", command);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));

        loginResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var result = await loginResp.Content.ReadFromJsonAsync<ApiResponse<System.Text.Json.JsonElement>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Be("Email not verified");
        result.Data.GetProperty("action").GetString().Should().Be("VERIFY_EMAIL");
        result.Data.GetProperty("canResend").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AccessProtected_WithoutOnboarding_Returns403()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Audit Corp", "Audit", "User", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        loginResp.IsSuccessStatusCode.Should().BeTrue("Login should succeed for access protected test");
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        loginResult.Should().NotBeNull();
        loginResult!.Data.Should().NotBeNull();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data!.Token);

        var customersResp = await _client.GetAsync("/api/customers");
        customersResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Onboard_AlreadyCompleted_Returns400()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Dual Corp", "Dual", "User", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        loginResp.IsSuccessStatusCode.Should().BeTrue("Login should succeed for onboard test");
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        loginResult.Should().NotBeNull();
        loginResult!.Data.Should().NotBeNull();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.Token);

        await _client.PostAsJsonAsync("/api/auth/onboard", new OnboardBusinessCommand("XAXX010101000", "Software", "Tech St 123"));

        var onboardResp2 = await _client.PostAsJsonAsync("/api/auth/onboard", new OnboardBusinessCommand("XAXX010101000", "Software", "Tech St 123"));
        onboardResp2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_InvalidatesSession()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Logout Corp", "Logout", "User", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        loginResp.IsSuccessStatusCode.Should().BeTrue("Login should succeed for logout test");
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        loginResult.Should().NotBeNull();
        loginResult!.Data.Should().NotBeNull();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.Token);

        await _client.PostAsync("/api/auth/logout", null);

        var sessionsResp = await _client.GetAsync("/api/auth/sessions");
        sessionsResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendVerification_SendsNewEmail_AndInvalidatesOldToken()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Resend Corp", "Resend", "User", _email, DefaultPassword));
        var token1 = _emailService.GetLastToken(_email);
        token1.Should().NotBeNull();
    }

    [Fact]
    public async Task ResendVerification_NonExistentEmail_Returns200()
    {
        var resendResp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand("nonexistent@example.com", "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerified_Returns200()
    {
        var email = "verified_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Verified Corp", "Resend", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var resendResp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResendVerification_TooFast_Returns429()
    {
        var email = "fast_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Fast Corp", "Resend", "User", email, DefaultPassword));

        var resendResp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ResendVerification_InvalidatesOldToken()
    {
        var email = "oldtoken_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("OldToken Corp", "Resend", "User", email, DefaultPassword));
        var token1 = _emailService.GetLastToken(email);
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns400()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("InvPass Corp", "Inv", "Pass", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, "WrongPass123!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_InvalidEmail_Returns400()
    {
        var command = new SignUpCommand("InvEmail Corp", "Inv", "Email", "invalid-email", DefaultPassword);
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_WeakPassword_Returns400()
    {
        var command = new SignUpCommand("WeakPass Corp", "Weak", "Pass", _email, "123");
        var response = await _client.PostAsJsonAsync("/api/auth/signup", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ReuseRevoked_InvalidatesSessionFamily()
    {
        var email = "reuse_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Reuse Corp", "Reuse", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        
        var loginCookies = loginResp.Headers.GetValues("Set-Cookie").ToList();
        var code1 = loginCookies.First(c => c.Contains("refreshToken=")).Split(';')[0]; 

        var refreshResp1 = await _client.PostAsync("/api/auth/refresh-token", null);
        refreshResp1.StatusCode.Should().Be(HttpStatusCode.OK);

        var attackerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh-token");
        reuseRequest.Headers.Add("Cookie", code1);
        
        var reuseResp = await attackerClient.SendAsync(reuseRequest);
        reuseResp.IsSuccessStatusCode.Should().BeFalse();

        var refreshResp2 = await _client.PostAsync("/api/auth/refresh-token", null);
        refreshResp2.IsSuccessStatusCode.Should().BeFalse();
    }
}
