using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Request to re-trigger a previously failed payment.
/// Scoped to a specific attempt to ensure idempotency across multiple retries.
/// </summary>
public class RetryPaymentRequest
{
    /// <summary>
    /// The attempt number (1-based). Used for idempotency logic (key = payment_id + attempt).
    /// </summary>
    /// <example>1</example>
    [Required]
    [Range(1, 100)]
    [JsonPropertyName("attempt_number")]
    public int AttemptNumber { get; set; }
}
