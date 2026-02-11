using Cobryx.Application.Payments.Webhooks.Commands.ProcessWebhook;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Infrastructure.Persistence;
using Concordia;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cobryx.IntegrationTests.Webhooks;

[Collection("Sequential")]
public class WebhookIngestionTests : IClassFixture<CobryxWebApplicationFactory>
{
    private readonly CobryxWebApplicationFactory _factory;

    public WebhookIngestionTests(CobryxWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProcessWebhookCommand_ShouldIngestEvent_AndDetectDuplicates()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var dbContext = scope.ServiceProvider.GetRequiredService<CobryxDbContext>();

        var provider = "Stripe";
        var externalEventId = Guid.NewGuid().ToString();
        var payload = "{\"test\": true}";

        var command = new ProcessWebhookCommand(provider, externalEventId, payload);

        var firstResult = await sender.Send(command);

        firstResult.IsSuccess.Should().BeTrue();

        var ingestedEvent = dbContext.WebhookEvents
            .FirstOrDefault(e => e.Provider == provider && e.ExternalEventId == externalEventId);

        ingestedEvent.Should().NotBeNull();
        ingestedEvent!.Status.Should().Be(WebhookStatus.Pending);

        var secondResult = await sender.Send(command);

        secondResult.IsSuccess.Should().BeTrue();

        var count = dbContext.WebhookEvents
            .Count(e => e.Provider == provider && e.ExternalEventId == externalEventId);

        count.Should().Be(1);
    }
}
