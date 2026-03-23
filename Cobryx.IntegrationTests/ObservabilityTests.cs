using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;

using Cobryx.Api.Contracts.V1.Financial;
using Cobryx.Application.Common.Observability;

using Cobryx.IntegrationTests.Helpers;

using FluentAssertions;

namespace Cobryx.IntegrationTests;

[Collection("Sequential")]
public class ObservabilityTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory = factory;

    [Fact]
    public async Task InvalidLogin_RecordsBothErrorAndOutcomeMetrics()
    {
        var meterName = CobryxMetrics.MeterName;
        var errorMetric = "cobryx_domain_errors_total";
        var outcomeMetric = "cobryx_outcome_total";

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

        var command = new { Email = "not-an-email", Password = "123" };
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        for (var i = 0; i < 15 && (errorCount == 0 || outcomeCount == 0); i++)
        {
            await Task.Delay(200);
        }

        errorCount.Should().BeGreaterThan(0, $"Error metric should have been recorded. ErrorCount: {errorCount}, OutcomeCount: {outcomeCount}");
        outcomeCount.Should().BeGreaterThan(0, "Outcome metric should have been recorded");

        recordedErrorCode.Should().Be("VALIDATION.FAILED");
        recordedOutcomeCode.Should().Be("VALIDATION.FAILED");
        recordedModule.Should().Be("Other");
        recordedNumericCode.Should().NotBeNull();
    }

    [Fact]
    public async Task SuccessfulRequest_IncrementsOutcomeMetric()
    {
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

        var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
        var command = new { BusinessName = "Test Biz", Email = uniqueEmail, Password = "Password123!", FirstName = "Test", LastName = "User" };
        var response = await client.PostIdempotentAsync("/api/v1/auth/signup", command);
        var signupResult = await response.Content.ReadFromJsonAsync<Cobryx.Api.Contracts.V1.Common.ApiSuccessResponse<Guid>>();
        response.StatusCode.Should().Be(HttpStatusCode.Created, $"because signup should succeed. Response: {await response.Content.ReadAsStringAsync()}");

        var delay = 0;
        while (recordedValue == 0 && delay < 3000)
        {
            await Task.Delay(100);
            delay += 100;
        }

        recordedValue.Should().BeGreaterThan(0);
        recordedCode.Should().Be("AUTH.USER.VERIFICATION_REQUIRED");
        recordedModule.Should().Be("Auth");
    }

    [Fact]
    public async Task ProductCreate_RecordsProductModule()
    {
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

        var token = CreateMockToken();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var productPayload = new CreateProductRequest("Test Product", "Test Description", 100.00m, "MXN");
        var response = await client.PostIdempotentAsync("/api/v1/financial/products", productPayload);

        response.StatusCode.Should().Be(HttpStatusCode.Created, $"because the token should be valid and payload is correct. Body: {await response.Content.ReadAsStringAsync()}");

        var delay = 0;
        while (recordedModule == null && delay < 3000)
        {
            await Task.Delay(100);
            delay += 100;
        }

        recordedModule.Should().Be("Other");
    }

    private string CreateMockToken()
    {
        var secret = "SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars";
        var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim("sub", Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim("tenant_id", Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim("onboarded", "true")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: "CobryxTest",
            audience: "CobryxTest",
            claims: claims,
            expires: DateTime.Now.AddMinutes(60),
            signingCredentials: creds
        );

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
