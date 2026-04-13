using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Used by POST /api/Payment/update to record an additional receipt
/// against an existing pending or partial payment.
/// </summary>
public class PaymentUpdateDto
{
    [Required]
    public int PaymentId { get; set; }

    /// <summary>Admin user performing/approving the update.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>New amount being added in this payment instalment.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "amount_received must be greater than zero.")]
    public decimal AmountReceived { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
