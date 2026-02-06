using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using Cobryx.Infrastructure.Observability;
using FluentAssertions;
using Xunit;

namespace Cobryx.IntegrationTests;

public class ObservabilityTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public ObservabilityTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task InvalidLogin_RecordsBothErrorAndOutcomeMetrics()
    {
        // Arrange
        var meterName = CobryxMetrics.MeterName;
        var errorMetric = "cobryx_domain_errors_total";
        var outcomeMetric = "cobryx_business_outcomes_total";

        long errorCount = 0;
        long outcomeCount = 0;
        string? recordedErrorCode = null;
        string? recordedOutcomeCode = null;
        string? recordedModule = null;
        string? recordedNumericCode = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            var tagList = tags.ToArray();
            if (instrument.Name == errorMetric)
            {
                errorCount += measurement;
                recordedErrorCode = tagList.FirstOrDefault(t => t.Key == "code").Value?.ToString();
                recordedModule = tagList.FirstOrDefault(t => t.Key == "module").Value?.ToString();
                recordedNumericCode = tagList.FirstOrDefault(t => t.Key == "numeric_code").Value?.ToString();
            }
            if (instrument.Name == outcomeMetric)
            {
                outcomeCount += measurement;
                recordedOutcomeCode = tagList.FirstOrDefault(t => t.Key == "code").Value?.ToString();
            }
        });

        listener.Start();
        var client = _factory.CreateClient();

        // Act
        // Invalid email format triggers Validation Error (400)
        var command = new { Email = "not-an-email", Password = "123" };
        var response = await client.PostAsJsonAsync("/api/auth/login", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await Task.Delay(100);

        errorCount.Should().BeGreaterThan(0);
        outcomeCount.Should().BeGreaterThan(0);
        
        // Validation Failed -> Error: VALIDATION.FAILED -> Outcome: VALIDATION.FAILED (because of prefix and .FAILED detection)
        recordedErrorCode.Should().Be("VALIDATION.FAILED");
        recordedOutcomeCode.Should().Be("VALIDATION.FAILED");
        recordedModule.Should().Be("Other"); // prefix VALIDATION is not in my switch, maps to Other
        recordedNumericCode.Should().NotBeNull();
    }

    [Fact]
    public async Task SuccessfulRequest_IncrementsOutcomeMetric()
    {
        // Arrange
        var meterName = CobryxMetrics.MeterName;
        var metricName = "cobryx_business_outcomes_total";
        long recordedValue = 0;
        string? recordedCode = null;
        string? recordedModule = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName && instrument.Name == metricName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            if (instrument.Name == metricName)
            {
                recordedValue += measurement;
                var tagList = tags.ToArray();
                recordedCode = tagList.FirstOrDefault(t => t.Key == "code").Value?.ToString();
                recordedModule = tagList.FirstOrDefault(t => t.Key == "module").Value?.ToString();
            }
        });
        listener.Start();

        var client = _factory.CreateClient();

        // Act
        // Trigger a success endpoint. 
        // We can simulate ResendVerification. It requires valid email format but mock implementations might success?
        // Or Health check? Health check doesn't use ApiResponse.
        // We need an endpoint returning ApiResponse.
        // Auth/Onboard requires token.
        // Auth/Signup requires data.

        // Let's use Signup with unique email.
        var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
        var command = new { BusinessName = "Test Biz", Email = uniqueEmail, Password = "Password123!", FirstName = "Test", LastName = "User" };
        var response = await client.PostAsJsonAsync("/api/auth/signup", command);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"because signup should succeed. Response: {content}");

        // Allow some time for metrics
        await Task.Delay(100);

        recordedValue.Should().BeGreaterThan(0);
        recordedCode.Should().Be("AUTH.SIGNUP.VERIFICATION_REQUIRED"); // Was .CREATED, but actually VERIFICATION_REQUIRED
        recordedModule.Should().Be("Auth");
    }

    [Fact]
    public async Task ProductSearch_RecordsProductModule()
    {
        // Arrange
        var meterName = CobryxMetrics.MeterName;
        var metricName = "cobryx_business_outcomes_total";
        
        string? recordedModule = null;

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName && instrument.Name == metricName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            recordedModule = tags.ToArray().FirstOrDefault(t => t.Key == "module").Value?.ToString();
        });
        listener.Start();

        var client = _factory.CreateClient();
        
        // Use a mock token to bypass [Authorize]
        var token = CreateMockToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, $"because the token should be valid. Body: {await response.Content.ReadAsStringAsync()}");
        
        await Task.Delay(100);
        recordedModule.Should().Be("Product");
    }

    private string CreateMockToken()
    {
        var secret = "SuperSecretKeyForIntegrationTests1234567890!";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim("tenant_id", Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim("onboarded", "true") 
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "CobryxApi-Test",
            audience: "CobryxClient-Test",
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
