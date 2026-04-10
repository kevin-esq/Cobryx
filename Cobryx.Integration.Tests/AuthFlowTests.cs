using System.Net;
using System.Net.Http.Json;

using Cobryx.Api.Contracts.V1.Identity;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Auth.Commands.Core;
using Cobryx.Application.Auth.Commands.Login;
using Cobryx.Application.Auth.Commands.Register;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Integration.Tests.Fakes;
using Cobryx.Integration.Tests.Helpers;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Integration.Tests;

[Collection("Sequential")]
public class AuthFlowTests : IClassFixture<CobryxWebApplicationFactory>, IAsyncLifetime
{
    private readonly CobryxWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly MockEmailService _emailService;
    private const string DefaultPassword = "Password123!@#";


    public AuthFlowTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        _emailService = factory.Services.GetRequiredService<MockEmailService>();
    }

    private static string GetUniqueEmail() => $"user_{Guid.NewGuid():N}@example.com";

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

    [Fact]
    public async Task HappyPath_SignupToOnboard_Success()
    {
        var email = GetUniqueEmail();
        var command = new SignUpCommand(
            "John Business",
            "John",
            "Doe",
            email,
            DefaultPassword,
            false,
            "v1.0"
        );
        var signupResp = await _client.PostIdempotentAsync("/api/v1/auth/signup", command);
        signupResp.StatusCode.Should().Be(HttpStatusCode.Created);
        var signupResult = await signupResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<Guid>>();
        signupResult!.OutcomeCode.Should().Be(AuthOutcomes.SignupVerificationRequired);

        var token = _emailService.GetLastToken(email);
        token.Should().NotBeNull();

        var verifyResp = await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse>();
        verifyResult!.OutcomeCode.Should().Be(AuthOutcomes.EmailVerified);

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        loginResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();
        loginResult!.Data!.AccessToken.Should().NotBeNull();
        loginResult.Data.RequiresOnboarding.Should().BeTrue();
        loginResult.OutcomeCode.Should().Be(AuthOutcomes.LoginCompleted);

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);

        var onboardReq = new OnboardRequest(
            "XAXX010101000",
            "Software",
            new AddressContract("Tech St", "123", "1A", "Silicon Valley", "12345", "Meta City", "Future State")
        );
        var onboardResp = await _client.PostIdempotentAsync("/api/v1/auth/onboard", onboardReq);
        if (onboardResp.StatusCode != HttpStatusCode.OK)
        {
            var errorJson = await onboardResp.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG: Onboard Error: {errorJson}");
        }
        onboardResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsResp = await _client.GetAsync("/api/v1/sessions");
        sessionsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResp = await _client.PostAsync("/api/v1/auth/refresh-token", null);
        refreshResp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithoutEmailVerification_Returns401WithCollapsedError()
    {
        var email = GetUniqueEmail();
        var command = new SignUpCommand("Any Corp", "A", "B", email, DefaultPassword, false, "v1.0");
        await _client.PostIdempotentAsync("/api/v1/auth/signup", command);

        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));

        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await loginResp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("message", out _).Should().BeFalse();
        root.GetProperty("errorCode").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Login_InvalidPassword_Returns401()
    {
        var email = GetUniqueEmail();
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("InvPass Corp", "Inv", "Pass", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostIdempotentAsync("/api/v1/auth/login", new LoginCommand(email, "WrongPass123!"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var json = await loginResp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("AUTH.INVALID_CREDENTIALS");
    }

    [Fact]
    public async Task Signup_InvalidEmail_Returns400()
    {
        var command = new SignUpCommand("InvEmail Corp", "Inv", "Email", "invalid-email", DefaultPassword);
        var response = await _client.PostIdempotentAsync("/api/v1/auth/signup", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Signup_WeakPassword_Returns400()
    {
        var email = GetUniqueEmail();
        var command = new SignUpCommand("WeakPass Corp", "Weak", "Pass", email, "123");
        var response = await _client.PostIdempotentAsync("/api/v1/auth/signup", command);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_ReuseRevoked_InvalidatesSessionFamily()
    {
        var email = "reuse_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Reuse Corp", "Reuse", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostIdempotentAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();

        var loginCookies = loginResp.Headers.GetValues("Set-Cookie").ToList();
        var code1 = loginCookies.First(c => c.Contains("refreshToken=")).Split(';')[0];

        var refreshResp1 = await _client.PostAsync("/api/v1/auth/refresh-token", null);
        refreshResp1.StatusCode.Should().Be(HttpStatusCode.OK);

        var attackerClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh-token");
        reuseRequest.Headers.Add("Cookie", code1);

        var reuseResp = await attackerClient.SendAsync(reuseRequest);
        reuseResp.IsSuccessStatusCode.Should().BeFalse();

        var refreshResp2 = await _client.PostAsync("/api/v1/auth/refresh-token", null);
        refreshResp2.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Onboard_Unauthorized_Returns403()
    {
        var email = GetUniqueEmail();
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Fail Corp", "Fail", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));

        var loginResp = await _client.PostIdempotentAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();
        loginResult!.Data!.AccessToken.Should().NotBeNull();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.Data.AccessToken);

        var customersResp = await _client.GetAsync("/api/v1/customers");
        customersResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Onboard_AlreadyCompleted_Returns409()
    {
        var email = GetUniqueEmail();
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Dual Corp", "Dual", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));
        var loginResp = await _client.PostIdempotentAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        var onboardReq = new OnboardRequest(
            "XAXX010101000",
            "Software",
            new AddressContract("Tech St", "123", "1A", "Silicon Valley", "12345", "Meta City", "Future State")
        );
        await _client.PostIdempotentAsync("/api/v1/auth/onboard", onboardReq);

        var onboardResp2 = await _client.PostIdempotentAsync("/api/v1/auth/onboard", onboardReq);
        if (onboardResp2.StatusCode != HttpStatusCode.Conflict)
        {
            var errorJson = await onboardResp2.Content.ReadAsStringAsync();
            Console.WriteLine($"DEBUG: Onboard2 Error: {errorJson}");
        }
        onboardResp2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Logout_InvalidatesSession()
    {
        var email = GetUniqueEmail();
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Logout Corp", "Logout", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));
        var loginResp = await _client.PostIdempotentAsync("/api/v1/auth/login", new LoginCommand(email, DefaultPassword));
        var loginResult = await loginResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<AuthResponseContract>>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.Data!.AccessToken);

        await _client.PostAsync("/api/v1/auth/logout", null);

        var sessionsResp = await _client.GetAsync("/api/v1/sessions");
        sessionsResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ResendVerification_NonExistentEmail_Returns200()
    {
        var resendResp = await _client.PostIdempotentAsync("/api/v1/auth/resend-verification", new ResendVerificationCommand("nonexistent@example.com", "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resendResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse>();
        result!.OutcomeCode.Should().Be(AuthOutcomes.VerificationEmailSent);
    }

    [Fact]
    public async Task ResendVerification_AlreadyVerified_Returns200()
    {
        var email = "verified_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Verified Corp", "Resend", "User", email, DefaultPassword));
        var token = _emailService.GetLastToken(email);
        await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token!));

        var resendResp = await _client.PostIdempotentAsync("/api/v1/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resendResp.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse>();
        result!.OutcomeCode.Should().Be(AuthOutcomes.VerificationEmailSent);
    }

    [Fact]
    public async Task ResendVerification_TooFast_Returns429()
    {
        var email = "fast_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("Fast Corp", "Resend", "User", email, DefaultPassword));

        var resp = await _client.PostIdempotentAsync("/api/v1/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resp.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("errorCode").GetString().Should().Be("SYSTEM.TOO_MANY_REQUESTS");
    }

    [Fact]
    public async Task ResendVerification_InvalidatesOldToken()
    {
        var email = "oldtoken_" + Guid.NewGuid().ToString("N") + "@example.com";
        await _client.PostIdempotentAsync("/api/v1/auth/signup", new SignUpCommand("OldToken Corp", "Resend", "User", email, DefaultPassword));
        var token1 = _emailService.GetLastToken(email);
        token1.Should().NotBeNull();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var user = await db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == (EmailAddress)email);
            db.Entry(user).Property(u => u.LastVerificationSentAt).CurrentValue = DateTime.UtcNow.AddMinutes(-5);
            await db.SaveChangesAsync();
        }

        var resendResp = await _client.PostIdempotentAsync("/api/v1/auth/resend-verification", new ResendVerificationCommand(email, "MOCK_CAPTCHA_TOKEN"));
        resendResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var token2 = _emailService.GetLastToken(email);
        token2.Should().NotBe(token1);

        var verifyResp = await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token1!));
        verifyResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var verifyResp2 = await _client.PostIdempotentAsync("/api/v1/auth/verify-email", new VerifyEmailCommand(token2!));
        verifyResp2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

}
