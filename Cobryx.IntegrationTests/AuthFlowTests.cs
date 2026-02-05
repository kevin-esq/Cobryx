using System.Net;
using System.Net.Http.Json;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Common;
using Cobryx.Application.Tenants.Commands.OnboardBusiness;
using Cobryx.Application.Common.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Cobryx.IntegrationTests.Fakes;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Cobryx.Domain.ValueObjects;

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
        var signupResult = await signupResp.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        signupResult!.Code.Should().Be(AuthOutcomes.SignupVerificationRequired);

        var token = _emailService.GetLastToken(_email);
        token.Should().NotBeNull();

        var verifyResp = await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResp.Content.ReadFromJsonAsync<ApiResponse>();
        verifyResult!.Code.Should().Be(AuthOutcomes.VerificationEmailVerified);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        loginResult!.Data!.Token.Should().NotBeNull();
        loginResult.Data.RequiresOnboarding.Should().BeTrue();
        loginResult.Code.Should().Be(AuthOutcomes.LoginSuccess);

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
    public async Task Login_WithoutEmailVerification_Returns401WithCollapsedError()
    {
        var command = new SignUpCommand("Any Corp", "A", "B", _email, DefaultPassword, false, "v1.0");
        await _client.PostAsJsonAsync("/api/auth/signup", command);

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));

        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await loginResp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("title").Should().NotBeNull();
        root.GetProperty("code").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("InvPass Corp", "Inv", "Pass", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, "WrongPass123!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await loginResp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("code").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
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

    [Fact]
    public async Task AccessProtected_WithoutOnboarding_Returns403()
    {
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Audit Corp", "Audit", "User", _email, DefaultPassword));
        var token = _emailService.GetLastToken(_email);
        await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login", new LoginCommand(_email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.Token);

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
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
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
        var loginResult = await loginResp.Content.ReadFromJsonAsync<ApiResponse<AuthResult>>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.Token);

        await _client.PostAsync("/api/auth/logout", null);

        var sessionsResp = await _client.GetAsync("/api/auth/sessions");
        sessionsResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendVerification_NonExistentEmail_Returns200()
    {
        var resendResp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand("nonexistent@example.com", "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resendResp.Content.ReadFromJsonAsync<ApiResponse>();
        result!.Code.Should().Be(AuthOutcomes.VerificationEmailSent);
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
        var result = await resendResp.Content.ReadFromJsonAsync<ApiResponse>();
        result!.Code.Should().Be(AuthOutcomes.VerificationEmailSent);
    }

    [Fact]
    public async Task ResendVerification_TooFast_Returns429()
    {
        var email = "fast_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("Fast Corp", "Resend", "User", email, DefaultPassword));

        // Attempt immediately (429) - SignUp already triggered the first send
        var resp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("code").GetString().Should().Be("SYSTEM.TOO_MANY_REQUESTS");
    }

    [Fact]
    public async Task ResendVerification_InvalidatesOldToken()
    {
        var email = "oldtoken_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostAsJsonAsync("/api/auth/signup", new SignUpCommand("OldToken Corp", "Resend", "User", email, DefaultPassword));
        var token1 = _emailService.GetLastToken(email);
        token1.Should().NotBeNull();

        // Backdate to allow resend (bypass 2-min limit)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == (EmailAddress)email);
            // Use ChangeTracker to bypass private set
            db.Entry(user).Property(u => u.LastVerificationSentAt).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        // Resend
        var resendResp = await _client.PostAsJsonAsync("/api/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var token2 = _emailService.GetLastToken(email);
        token2.Should().NotBe(token1);

        // Try verifying with old token (Failure - Maps to 400 with message)
        var verifyResp = await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token1!));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Try verifying with new token (Success)
        var verifyResp2 = await _client.PostAsJsonAsync("/api/auth/verify-email", new VerifyEmailCommand(token2!));
        verifyResp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

}
