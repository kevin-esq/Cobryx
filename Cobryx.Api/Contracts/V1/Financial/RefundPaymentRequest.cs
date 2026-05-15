using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Request to reverse a previously completed payment.
/// Supports partial refunds and requires a reason for audit traceability.
/// </summary>
public class RefundPaymentRequest
{
    /// <summary>
    /// The amount to refund (Decimal). If not provided, the full refundable balance will be used.
    /// </summary>
    /// <example>50.00</example>
    [Range(0.01, 1000000.00)]
    [JsonPropertyName("amount")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// ISO-4217 3-letter currency code. Must match the original payment.
    /// </summary>
    /// <example>USD</example>
    [Required]
    [StringLength(3, MinimumLength = 3)]
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Reason for the refund. Highly recommended for operational transparency.
    /// </summary>
    /// <example>Customer returned items.</example>
    [StringLength(500)]
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
