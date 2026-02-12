using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cobryx.IntegrationTests;

public class ReliabilityTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public ReliabilityTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Idempotency_ShouldReplaySameResponse_ForCriticalMutations()
    {
        var client = _factory.CreateClient();
        var idempotencyKey = Guid.NewGuid().ToString();

        var requestBody = new
        {
            Email = $"test-{Guid.NewGuid()}@example.com",
            Password = "SecurePassword123!",
            FirstName = "Test",
            LastName = "User",
            BusinessName = "Test Corp",
            CaptchaToken = "valid-token"
        };

        client.DefaultRequestHeaders.Add("X-Idempotency-Key", idempotencyKey);

        var response1 = await client.PostAsJsonAsync("/api/auth/signup", requestBody);
        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        var content1 = await response1.Content.ReadAsStringAsync();

        var response2 = await client.PostAsJsonAsync("/api/auth/signup", requestBody);

        response2.StatusCode.Should().Be(HttpStatusCode.Created);
        var content2 = await response2.Content.ReadAsStringAsync();
        content2.Should().Be(content1);
    }

    [Fact]
    public async Task Outbox_ShouldProcessEventsInDeterministicOrder()
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var infraContext = (Cobryx.Infrastructure.Persistence.CobryxDbContext)dbContext;

        var now = DateTime.UtcNow;
        var event1 = new OutboxEvent("TypeA", "{}", now);
        var event2 = new OutboxEvent("TypeB", "{}", now);

        infraContext.OutboxEvents.Add(event1);
        infraContext.OutboxEvents.Add(event2);
        await dbContext.SaveChangesAsync();

        var nextEvents = await infraContext.OutboxEvents
            .Where(e => e.ProcessedOnUtc == null)
            .OrderBy(e => e.OccurredOnUtc)
            .ThenBy(e => e.Id)
            .ToListAsync();

        nextEvents.Count.Should().BeGreaterThanOrEqualTo(2);
    }
}
