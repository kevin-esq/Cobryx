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

public class HandleWebhookEventHandler(
    IWebhookEventRepository webhookEventRepository,
    IPaymentRepository paymentRepository,
    ITenantRepository tenantRepository,
    IUnitOfWork unitOfWork,
    IEnumerable<IWebhookParser> parsers,
    ISender sender,
    ILogger<HandleWebhookEventHandler> logger) : IRequestHandler<HandleWebhookEventCommand, Result>
{
    private readonly IWebhookEventRepository _webhookEventRepository = webhookEventRepository;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly ITenantRepository _tenantRepository = tenantRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IEnumerable<IWebhookParser> _parsers = parsers;
    private readonly ISender _sender = sender;
    private readonly ILogger<HandleWebhookEventHandler> _logger = logger;

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
        var stripeAccountId = (parseResult.Metadata != null && parseResult.Metadata.TryGetValue("stripe_account_id", out var accountId))
            ? accountId
            : null;

        var tenantId = Guid.Empty;

        if (!string.IsNullOrEmpty(stripeAccountId))
        {
            var tenant = await _tenantRepository.GetByStripeAccountIdAsync(stripeAccountId, ct);
            tenantId = tenant?.Id ?? Guid.Empty;
        }

        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("Could not resolve Tenant for Stripe Account {StripeAccountId}", stripeAccountId ?? "Missing");
            return Result.Failure(DomainErrorCode.Tenant.NotFound);
        }

        decimal amount = data.GetProperty("amount").GetInt64() / 100m;
        string currency = data.GetProperty("currency").GetString()?.ToUpper() ?? CobryxDefaults.Currency;
        string status = data.GetProperty("status").GetString() ?? "unknown";

        var command = new RecordPayoutCommand(
            tenantId,
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
