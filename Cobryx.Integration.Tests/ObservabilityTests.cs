using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Cobryx.Api.Contracts.V1.Financial;
using Cobryx.Application.Common.Observability;
using Cobryx.Integration.Tests.Helpers;

using FluentAssertions;

namespace Cobryx.Integration.Tests
{
    [Collection("Sequential")]
    public class ObservabilityTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
    {
        [Fact]
        public async Task InvalidLoginRecordsBothErrorAndOutcomeMetrics()
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
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == meterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };

            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
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
            var client = factory.CreateClient();

            var command = new { Email = "not-an-email", Password = "123" };
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", command);

            _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            for (var i = 0; i < 15 && (errorCount == 0 || outcomeCount == 0); i++)
            {
                await Task.Delay(200);
            }

            _ = errorCount.Should().BeGreaterThan(0,
                $"Error metric should have been recorded. ErrorCount: {errorCount}, OutcomeCount: {outcomeCount}");
            _ = outcomeCount.Should().BeGreaterThan(0, "Outcome metric should have been recorded");

            _ = recordedErrorCode.Should().Be("VALIDATION.FAILED");
            _ = recordedOutcomeCode.Should().Be("VALIDATION.FAILED");
            _ = recordedModule.Should().Be("Other");
            _ = recordedNumericCode.Should().NotBeNull();
        }

        [Fact]
        public async Task SuccessfulRequestIncrementsOutcomeMetric()
        {
            var meterName = CobryxMetrics.MeterName;
            var metricName = "cobryx_business_outcomes_total";
            long recordedValue = 0;
            string? recordedCode = null;
            string? recordedModule = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == meterName && instrument.Name == metricName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((instrument, measurement, tags, _) =>
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

            var client = factory.CreateClient();

            var uniqueEmail = $"test-{Guid.NewGuid()}@example.com";
            var command = new
            {
                BusinessName = "Test Biz",
                Email = uniqueEmail,
                Password = "Password123!",
                FirstName = "Test",
                LastName = "User"
            };
            var response = await client.PostIdempotentAsync("/api/v1/auth/signup", command);
            _ = await response.Content.ReadFromJsonAsync<Api.Contracts.V1.Common.ApiSuccessResponse<Guid>>();
            _ = response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"because signup should succeed. Response: {await response.Content.ReadAsStringAsync()}");

            var delay = 0;
            while (recordedValue == 0 && delay < 3000)
            {
                await Task.Delay(100);
                delay += 100;
            }

            _ = recordedValue.Should().BeGreaterThan(0);
            _ = recordedCode.Should().Be("AUTH.USER.VERIFICATION_REQUIRED");
            _ = recordedModule.Should().Be("Auth");
        }

        [Fact]
        public async Task ProductCreateRecordsProductModule()
        {
            var meterName = CobryxMetrics.MeterName;
            var metricName = "cobryx_business_outcomes_total";

            string? recordedModule = null;

            using var listener = new MeterListener();
            listener.InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == meterName && instrument.Name == metricName)
                {
                    meterListener.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
            {
                recordedModule = tags.ToArray().FirstOrDefault(t => t.Key == "module").Value?.ToString();
            });
            listener.Start();

            var client = factory.CreateClient();

            var token = CreateMockToken();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var productPayload = new CreateProductRequest("Test Product", "Test Description", 100.00m, "MXN");
            var response = await client.PostIdempotentAsync("/api/v1/financial/products", productPayload);

            _ = response.StatusCode.Should().Be(HttpStatusCode.Created,
                $"because the token should be valid and payload is correct. Body: {await response.Content.ReadAsStringAsync()}");

            var delay = 0;
            while (recordedModule == null && delay < 3000)
            {
                await Task.Delay(100);
                delay += 100;
            }

            _ = recordedModule.Should().Be("Other");
        }

        private static string CreateMockToken()
        {
            var secret = "SuperSecretKeyForTesting!LengthMustBeAtLeast32Chars";
            var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(secret));
            var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(key,
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

            Claim[] claims =
            [
                new("sub", Guid.NewGuid().ToString()),
                new("tenant_id", Guid.NewGuid().ToString()),
                new("onboarded", "true")
            ];

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
}
