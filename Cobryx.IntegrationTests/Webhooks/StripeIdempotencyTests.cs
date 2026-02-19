using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Subscriptions.Services;
using Cobryx.Domain.Entities;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;
using Cobryx.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cobryx.IntegrationTests.Webhooks;

[Collection("Sequential")]
public class StripeIdempotencyTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public StripeIdempotencyTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HandleCheckoutCompletedAsync_ShouldBeIdempotent()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<StripeSubscriptionSyncService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var now = clock.UtcNow;

        var tenantId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var stripeEventId = "evt_test_" + Guid.NewGuid();
        var stripeCustomerId = "cus_test";
        var stripeSubscriptionId = "sub_test";

        // Pre-seed tenant and subscription
        var tenant = new Tenant("Test Business", "test@test.com");
        typeof(Tenant).GetProperty("Id")!.SetValue(tenant, tenantId);
        dbContext.Tenants.Add(tenant);

        var plan = new SubscriptionPlan("Pro", "Pro Plan", new Money(499, "MXN"), 500, 10, 500, PlanTier.Pro, 14, "price_test");
        typeof(SubscriptionPlan).GetProperty("Id")!.SetValue(plan, planId);
        dbContext.SubscriptionPlans.Add(plan);

        var subscription = new TenantSubscription(tenantId, planId, now);
        dbContext.TenantSubscriptions.Add(subscription);
        await dbContext.SaveChangesAsync();

        // Act - First call
        await syncService.HandleCheckoutCompletedAsync(stripeEventId, stripeCustomerId, stripeSubscriptionId, tenantId);
        
        // Assert - Event recorded
        var processedEvent = await dbContext.ProcessedStripeEvents.FirstOrDefaultAsync(e => e.StripeEventId == stripeEventId);
        processedEvent.Should().NotBeNull();

        var initialProcessedAt = processedEvent!.ProcessedAtUtc;

        // Act - Second call (duplicate)
        await syncService.HandleCheckoutCompletedAsync(stripeEventId, stripeCustomerId, stripeSubscriptionId, tenantId);

        // Assert - Should not have changed or crashed
        var processedEventAfter = await dbContext.ProcessedStripeEvents.FirstOrDefaultAsync(e => e.StripeEventId == stripeEventId);
        processedEventAfter!.ProcessedAtUtc.Should().Be(initialProcessedAt);
        
        // Verify only one event exists
        var count = await dbContext.ProcessedStripeEvents.CountAsync(e => e.StripeEventId == stripeEventId);
        count.Should().Be(1);
    }
}
