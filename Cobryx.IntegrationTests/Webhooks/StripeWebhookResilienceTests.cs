using System.Net;
using System.Net.Http.Json;
using Cobryx.Api.Contracts.V1.Common;
using FluentAssertions;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Cobryx.Infrastructure.Persistence;
using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cobryx.Api.Outcomes;
using Cobryx.Application.Common.Interfaces;

namespace Cobryx.IntegrationTests.Webhooks;

[Collection("Sequential")]
public class StripeWebhookResilienceTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CobryxWebApplicationFactory _factory;
    private const string WebhookSecret = "whsec_test_123";

    public StripeWebhookResilienceTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task StripeReceive_ShouldHandleConcurrentRequests_Gracefully()
    {
        // Arrange
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
            
            var plan = new SubscriptionPlan("Pro", "Pro", new Money(100, "MXN"), 100, 10, Cobryx.Domain.Enums.PlanTier.Pro, 0, "price_test");
            db.SubscriptionPlans.Add(plan);
            
            var sub = new TenantSubscription(tenant.Id, plan.Id, DateTime.UtcNow);
            typeof(TenantSubscription).GetProperty("StripeSubscriptionId")!.SetValue(sub, "sub_test_race");
            typeof(TenantSubscription).GetProperty("StripeCustomerId")!.SetValue(sub, "cus_test");
            db.TenantSubscriptions.Add(sub);
            await db.SaveChangesAsync();
        }

        // Act
        var task1 = SendWebhookAsync(json);
        var task2 = SendWebhookAsync(json);

        await Task.WhenAll(task1, task2);

        // Assert
        task1.Result.StatusCode.Should().Be(HttpStatusCode.OK);
        task2.Result.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify only one event recorded
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
            var count = await db.Set<ProcessedStripeEvent>().CountAsync(e => e.StripeEventId == stripeEventId);
            count.Should().Be(1);
        }
    }

    [Fact]
    public async Task StripeReceive_ShouldReturn500_WhenProcessingFails()
    {
        // Arrange
        var stripeEventId = "evt_fail_" + Guid.NewGuid();
        var json = @"{ ""id"": """ + stripeEventId + @""", ""object"": ""event"", ""type"": ""customer.subscription.updated"" }";
        
        // Act
        var response = await SendWebhookAsync(json);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        error!.ErrorCode.Should().Be(WebhookOutcomes.ProcessingFailed);
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(string json)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/webhooks/stripe");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var signedPayload = $"{timestamp}.{json}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload));
        var signature = BitConverter.ToString(hash).Replace("-", "").ToLower();
        
        request.Headers.Add("Stripe-Signature", $"t={timestamp},v1={signature}");
        
        return await _client.SendAsync(request);
    }
}
