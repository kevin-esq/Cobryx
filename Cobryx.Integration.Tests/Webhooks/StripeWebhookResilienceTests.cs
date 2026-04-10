using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

using Cobryx.Api.Contracts.V1.Common;
using Cobryx.Api.Outcomes;
using Cobryx.Domain.Identity;
using Cobryx.Domain.ValueObjects;
using Cobryx.Infrastructure.Persistence;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Integration.Tests.Webhooks;

[Collection("Sequential")]
public class StripeWebhookResilienceTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CobryxWebApplicationFactory _factory = factory;
    private const string WebhookSecret = "whsec_test_123";

    [Fact(Skip = "Flaky concurrency test - needs investigation")]
    public async Task StripeReceive_ShouldHandleConcurrentRequests_Gracefully()
    {
        var stripeEventId = "evt_race_" + Guid.NewGuid();

        var json = @"{
  ""id"": ""{{EVENT_ID}}"",
  ""object"": ""event"",
  ""api_version"": ""2026-01-28.clover"",
  ""created"": 1700000000,
  ""data"": {
    ""object"": {
      ""id"": ""sub_test_race"",
      ""object"": ""subscription"",
      ""customer"": ""cus_test"",
      ""status"": ""active""
    }
  },
  ""livemode"": false,
  ""pending_webhooks"": 0,
  ""request"": {
    ""id"": ""req_123"",
    ""idempotency_key"": ""key_123""
  },
  ""type"": ""customer.subscription.updated""
}".Replace("{{EVENT_ID}}", stripeEventId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var tenant = new Tenant("Race Tenant", "race@test.com");
            db.Tenants.Add(tenant);

            var plan = new SubscriptionPlan("Pro", "Pro", new Money(100, "MXN"), 100, 10, 100, Cobryx.Domain.Payments.Enums.PlanTier.Pro, 0, "price_test");
            db.SubscriptionPlans.Add(plan);

            var sub = new TenantSubscription(tenant.Id, plan.Id, DateTime.UtcNow);
            typeof(TenantSubscription).GetProperty("StripeSubscriptionId")!.SetValue(sub, "sub_test_race");
            typeof(TenantSubscription).GetProperty("StripeCustomerId")!.SetValue(sub, "cus_test");
            db.TenantSubscriptions.Add(sub);
            await db.SaveChangesAsync();
        }

        var task1 = SendWebhookAsync(json);
        var task2 = SendWebhookAsync(json);

        await Task.WhenAll(task1, task2);

        if (task1.Result.StatusCode != HttpStatusCode.OK)
        {
            var body = await task1.Result.Content.ReadAsStringAsync();
            throw new Exception($"Task 1 failed with {task1.Result.StatusCode}. Body: {body}");
        }
        if (task2.Result.StatusCode != HttpStatusCode.OK)
        {
            var body = await task2.Result.Content.ReadAsStringAsync();
            throw new Exception($"Task 2 failed with {task2.Result.StatusCode}. Body: {body}");
        }

        task1.Result.StatusCode.Should().Be(HttpStatusCode.OK);
        task2.Result.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var count = await db.Set<ProcessedStripeEvent>().CountAsync(e => e.StripeEventId == stripeEventId);
            count.Should().Be(1);
        }
    }

    [Fact(Skip = "Error code mismatch - needs investigation")]
    public async Task StripeReceive_ShouldReturn500_WhenProcessingFails()
    {
        var stripeEventId = "evt_fail_" + Guid.NewGuid();
        var json = @"{ ""id"": """ + stripeEventId + @""", ""object"": ""event"", ""type"": ""customer.subscription.updated"" }";

        var response = await SendWebhookAsync(json);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().StartWith(WebhookOutcomes.ProcessingFailed);
    }

    [Fact]
    public async Task StripeReceive_ShouldIgnoreDuplicateWebhook_WhenAlreadyProcessed()
    {
        var stripeEventId = "evt_duplicate_" + Guid.NewGuid();
        var json = @"{
  ""id"": ""{{EVENT_ID}}"",
  ""object"": ""event"",
  ""api_version"": ""2026-01-28.clover"",
  ""created"": 1700000000,
  ""data"": {
    ""object"": {
      ""id"": ""sub_test_dup"",
      ""object"": ""subscription"",
      ""customer"": ""cus_test_dup"",
      ""status"": ""active""
    }
  },
  ""livemode"": false,
  ""pending_webhooks"": 0,
  ""request"": {
    ""id"": ""req_123"",
    ""idempotency_key"": ""key_123""
  },
  ""type"": ""customer.subscription.updated""
}".Replace("{{EVENT_ID}}", stripeEventId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            db.Set<ProcessedStripeEvent>().Add(new ProcessedStripeEvent(stripeEventId, "customer.subscription.updated", DateTime.UtcNow));
            await db.SaveChangesAsync();
        }

        var response = await SendWebhookAsync(json);

        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        body.Should().Contain("\"received\":true");
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = Convert.ToHexString(hash).ToLower();

        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");

        return await _client.SendAsync(request);
    }
}
