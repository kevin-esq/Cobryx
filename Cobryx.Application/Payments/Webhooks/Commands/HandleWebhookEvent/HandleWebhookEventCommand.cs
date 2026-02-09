using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;
using Cobryx.Application.Payments.Commands.RefundPayment;
using Cobryx.Application.Payments.Commands.HandleChargeback;
using System.Text.Json;

namespace Cobryx.Application.Payments.Webhooks.Commands.HandleWebhookEvent;

public record HandleWebhookEventCommand(Guid WebhookEventId) : IRequest<Result>;

public class HandleWebhookEventHandler : IRequestHandler<HandleWebhookEventCommand, Result>
{
    private readonly IWebhookEventRepository _webhookEventRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IWebhookParser> _parsers;
    private readonly ISender _sender;
    private readonly ILogger<HandleWebhookEventHandler> _logger;

    public HandleWebhookEventHandler(
        IWebhookEventRepository webhookEventRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IEnumerable<IWebhookParser> parsers,
        ISender sender,
        ILogger<HandleWebhookEventHandler> logger)
    {
        _webhookEventRepository = webhookEventRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _parsers = parsers;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result> Handle(HandleWebhookEventCommand request, CancellationToken cancellationToken)
    {
        var webhookEvent = await _webhookEventRepository.GetByIdAsync(request.WebhookEventId, cancellationToken);

        if (webhookEvent == null)
            return Result.Failure("Webhook event not found.");

        if (webhookEvent.Status == WebhookStatus.Processed)
            return Result.Success();

        var parser = _parsers.FirstOrDefault(p => p.Provider.Equals(webhookEvent.Provider, StringComparison.OrdinalIgnoreCase));
        if (parser == null)
        {
            webhookEvent.MarkAsFailed($"No parser found for provider: {webhookEvent.Provider}");
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure($"No parser found for provider: {webhookEvent.Provider}");
        }

        webhookEvent.StartProcessing();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            var parseResult = await parser.ParseAsync(webhookEvent.RawPayload);

            var result = await DispatchInternalCommandAsync(parseResult, cancellationToken);

            if (result.IsSuccess)
            {
                webhookEvent.MarkAsProcessed();
            }
            else
            {
                webhookEvent.MarkAsFailed(result.Error ?? "Unknown error during command dispatch.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook {Id}", webhookEvent.Id);
            webhookEvent.MarkAsFailed(ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> DispatchInternalCommandAsync(WebhookParseResult parseResult, CancellationToken ct)
    {
        if (parseResult.Data is not JsonElement data)
        {
            return Result.Failure("Invalid webhook data format.");
        }

        return parseResult.InternalEventType switch
        {
            "ChargeRefunded" => await HandleRefundAsync(parseResult, data, ct),
            "ChargeDisputeCreated" => await HandleChargebackAsync(parseResult, ct),
            _ => await HandleUnknownEventAsync(parseResult)
        };
    }

    private async Task<Result> HandleRefundAsync(WebhookParseResult parseResult, JsonElement data, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(parseResult.ExternalTransactionId))
            return Result.Failure("Missing ExternalTransactionId for refund.");

        var payment = await _paymentRepository.GetByReferenceAsync(parseResult.ExternalTransactionId, ct);
        if (payment == null)
            return Result.Failure($"Payment with reference {parseResult.ExternalTransactionId} not found.");

        decimal amount = data.GetProperty("amount_refunded").GetInt64() / 100m;
        string currency = data.GetProperty("currency").GetString()?.ToUpper() ?? "MXN";

        var command = new RefundPaymentCommand(payment.Id, amount, currency);
        return await _sender.Send(command, ct);
    }

    private async Task<Result> HandleChargebackAsync(WebhookParseResult parseResult, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(parseResult.ExternalTransactionId))
            return Result.Failure("Missing ExternalTransactionId for chargeback.");

        var payment = await _paymentRepository.GetByReferenceAsync(parseResult.ExternalTransactionId, ct);
        if (payment == null)
            return Result.Failure($"Payment with reference {parseResult.ExternalTransactionId} not found.");

        var command = new HandleChargebackCommand(payment.Id);
        return await _sender.Send(command, ct);
    }

    private Task<Result> HandleUnknownEventAsync(WebhookParseResult parseResult)
    {
        _logger.LogWarning("Translation for event type {InternalEventType} not implemented.", parseResult.InternalEventType);
        return Task.FromResult(Result.Failure($"Translation for {parseResult.InternalEventType} not implemented."));
    }
}
