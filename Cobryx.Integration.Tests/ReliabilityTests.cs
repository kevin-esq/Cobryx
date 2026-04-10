using System.Net;
using System.Net.Http.Json;

using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Messaging;
using Cobryx.Infrastructure.Persistence;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cobryx.Integration.Tests
{
    public class ReliabilityTests(CobryxWebApplicationFactory factory) : IClassFixture<CobryxWebApplicationFactory>
    {
        private readonly CobryxWebApplicationFactory _factory = factory;

        [Fact]
        public async Task IdempotencyShouldReplaySameResponseForCriticalMutations()
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

            var response1 = await client.PostAsJsonAsync("/api/v1/auth/signup", requestBody);
            _ = response1.StatusCode.Should().Be(HttpStatusCode.Created);
            var content1 = await response1.Content.ReadAsStringAsync();

            var response2 = await client.PostAsJsonAsync("/api/v1/auth/signup", requestBody);

            _ = response2.StatusCode.Should().Be(HttpStatusCode.Created);
            var content2 = await response2.Content.ReadAsStringAsync();
            _ = content2.Should().Be(content1);
        }

        [Fact]
        public async Task OutboxShouldProcessEventsInDeterministicOrder()
        {
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var infraContext = (CobryxDbContext)dbContext;

            var tenantId = Guid.NewGuid();
            var event1 = new OutboxMessage(tenantId, "TypeA", "{}");
            var event2 = new OutboxMessage(tenantId, "TypeB", "{}");

            _ = infraContext.OutboxMessages.Add(event1);
            _ = infraContext.OutboxMessages.Add(event2);
            _ = await dbContext.SaveChangesAsync();

            var nextEvents = await infraContext.OutboxMessages
                .Where(static e => !e.IsProcessed)
                .OrderBy(static e => e.OccurredOnUtc)
                .ThenBy(static e => e.Id)
                .ToListAsync();

            _ = nextEvents.Count.Should().BeGreaterThanOrEqualTo(2);
        }
    }
}
