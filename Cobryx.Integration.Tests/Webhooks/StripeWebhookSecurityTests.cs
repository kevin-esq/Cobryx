using System.Net;
using System.Net.Http.Json;

using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;

using FluentAssertions;

namespace Cobryx.Integration.Tests.Webhooks;

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
        var payload = new { type = "invoice.paid" };
        var request = JsonContent.Create(payload);

        var response = await _client.PostAsync("/api/v1/webhooks/stripe", request);

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
        var payload = new { type = "invoice.paid" };
        var request = JsonContent.Create(payload);
        request.Headers.Add("Stripe-Signature", "t=123,v1=invalid_signature");

        var response = await _client.PostAsync("/api/v1/webhooks/stripe", request);

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Expected 400, got {response.StatusCode}. Body: {body}");
        }

        var content = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        content!.ErrorCode.Should().Be(WebhookOutcomes.InvalidSignature);
    }
}
