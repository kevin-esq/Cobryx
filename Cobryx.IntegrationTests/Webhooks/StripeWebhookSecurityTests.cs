using System.Net;
using System.Net.Http.Json;
using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using FluentAssertions;
using Xunit;

namespace Cobryx.IntegrationTests.Webhooks;

[Collection("Sequential")]
public class StripeWebhookSecurityTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StripeWebhookSecurityTests(CobryxWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StripeReceive_ShouldReturnBadRequest_WhenSignatureIsMissing()
    {
        // Arrange
        var payload = new { type = "invoice.paid" };
        var request = JsonContent.Create(payload);

        // Act
        var response = await _client.PostAsync("/api/v1/webhooks/stripe", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected 400, got {response.StatusCode}. Body: {body}");
        }

        var content = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        content!.ErrorCode.Should().Be(WebhookOutcomes.InvalidSignature);
    }

    [Fact]
    public async Task StripeReceive_ShouldReturnBadRequest_WhenSignatureIsInvalid()
    {
        // Arrange
        var payload = new { type = "invoice.paid" };
        var request = JsonContent.Create(payload);
        request.Headers.Add("Stripe-Signature", "t=123,v1=invalid_signature");

        // Act
        var response = await _client.PostAsync("/api/v1/webhooks/stripe", request);

        // Assert
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected 400, got {response.StatusCode}. Body: {body}");
        }

        var content = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        content!.ErrorCode.Should().Be(WebhookOutcomes.InvalidSignature);
    }
}
