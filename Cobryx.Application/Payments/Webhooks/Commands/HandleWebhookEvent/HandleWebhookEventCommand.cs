using Cobryx.Application.Payments.Webhooks.Common;
using Cobryx.Application.Webhooks.Entities;
using Cobryx.Application.Webhooks.Interfaces;
using Cobryx.Application.Payments.Webhooks.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces;
using Concordia;
using Microsoft.Extensions.Logging;
using Cobryx.Application.Payments.Commands.RefundPayment;
using Cobryx.Application.Payments.Commands.HandleChargeback;
using Cobryx.Application.Admin.Commands.RecordPayout;
using Cobryx.Application.Payments.Webhooks.Commands.HandlePaymentFailed;
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
            return Result.Failure(DomainErrorCode.Webhooks.EventNotFound);

        if (webhookEvent.Status == WebhookStatus.Processed)
            return Result.Success();

        var parser = _parsers.FirstOrDefault(p => p.Provider.Equals(webhookEvent.Provider, StringComparison.OrdinalIgnoreCase));
        if (parser == null)
        {
            webhookEvent.MarkAsFailed(DomainErrorCode.Webhooks.ParserNotFound);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(DomainErrorCode.Webhooks.ParserNotFound);
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
                webhookEvent.MarkAsFailed(result.Error ?? CobryxDefaults.UnknownValue);
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
            return Result.Failure(DomainErrorCode.Webhooks.InvalidDataFormat);
        }

        return parseResult.InternalEventType switch
        {
            WebhookConstants.InternalEvents.ChargeRefunded => await HandleRefundAsync(parseResult, data, ct),
            WebhookConstants.InternalEvents.ChargeDisputeCreated => await HandleChargebackAsync(parseResult, ct),
            WebhookConstants.InternalEvents.PayoutPaid => await HandlePayoutAsync(parseResult, data, ct),
            WebhookConstants.InternalEvents.PayoutFailed => await HandlePayoutAsync(parseResult, data, ct),
            WebhookConstants.InternalEvents.PaymentFailed => await _sender.Send(new HandlePaymentFailedCommand(data, parseResult.InternalEventType), ct),
            WebhookConstants.InternalEvents.InvoicePaymentFailed => await _sender.Send(new HandlePaymentFailedCommand(data, parseResult.InternalEventType), ct),
            _ => await HandleUnknownEventAsync(parseResult)
        };
    }

    private async Task<Result> HandlePayoutAsync(WebhookParseResult parseResult, JsonElement data, CancellationToken ct)
    {
        var tenantId = Guid.Empty;
        // Payouts might be platform or tenant.
        // In Stripe Connect, the 'account' field of the event tells us the tenant.
        // For now, we attempt to find the tenant by their StripeAccountId if it's a connect event.
        // (This part needs a safe way to resolve tenantId from StripeAccountId)

        // MVP: Assuming platform for now or implementing basic lookup if metadata or account id is present.
        // In a real scenario, we'd lookup ITenantRepository.GetByStripeAccountIdAsync

        decimal amount = data.GetProperty("amount").GetInt64() / 100m;
        string currency = data.GetProperty("currency").GetString()?.ToUpper() ?? CobryxDefaults.Currency;
        string status = data.GetProperty("status").GetString() ?? "unknown";

        // Logic to resolve tenantId...
        // For payout.paid events, we record it.
        var command = new RecordPayoutCommand(
            tenantId, // TODO: Resolve from account id
            amount,
            currency,
            parseResult.ExternalTransactionId ?? "unknown",
            status);

        return await _sender.Send(command, ct);
    }

    private async Task<Result> HandleRefundAsync(WebhookParseResult parseResult, JsonElement data, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(parseResult.ExternalTransactionId))
            return Result.Failure(DomainErrorCode.Webhooks.MissingTransactionId);

        var payment = await _paymentRepository.GetByReferenceAsync(parseResult.ExternalTransactionId, ct);
        if (payment == null)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        decimal amount = data.GetProperty("amount_refunded").GetInt64() / 100m;
        string currency = data.GetProperty("currency").GetString()?.ToUpper() ?? CobryxDefaults.Currency;

        var command = new RefundPaymentCommand(payment.Id, amount, currency);
        return await _sender.Send(command, ct);
    }

    private async Task<Result> HandleChargebackAsync(WebhookParseResult parseResult, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(parseResult.ExternalTransactionId))
            return Result.Failure(DomainErrorCode.Webhooks.MissingTransactionId);

        var payment = await _paymentRepository.GetByReferenceAsync(parseResult.ExternalTransactionId, ct);
        if (payment == null)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        var command = new HandleChargebackCommand(payment.Id);
        return await _sender.Send(command, ct);
    }

    private Task<Result> HandleUnknownEventAsync(WebhookParseResult parseResult)
    {
        _logger.LogWarning("Translation for event type {InternalEventType} not implemented.", parseResult.InternalEventType);
        return Task.FromResult(Result.Failure(DomainErrorCode.System.NotAllowed));
    }
}
