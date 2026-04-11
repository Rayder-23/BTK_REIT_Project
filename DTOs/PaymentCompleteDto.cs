using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>
/// Carries optional payment details supplied when completing a transfer.
/// Only used when transfer_type is NOT 'gift' or 'inheritance'.
/// </summary>
public class PaymentCompleteDto
{
    /// <summary>Required. Must be 'bank-transfer', 'cheque', or 'cash'.</summary>
    [Required]
    [MaxLength(30)]
    public string PaymentType { get; set; } = null!;

    /// <summary>Tax amount. Defaults to 0.00 if omitted.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "tax cannot be negative.")]
    public decimal Tax { get; set; } = 0m;

    /// <summary>Any additional charges. Defaults to 0.00 if omitted.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "additional_payments cannot be negative.")]
    public decimal AdditionalPayments { get; set; } = 0m;

    /// <summary>Amount already paid at time of transfer completion. Defaults to 0.00.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "amount_paid cannot be negative.")]
    public decimal AmountPaid { get; set; } = 0m;

    /// <summary>Issuing bank name (optional, e.g. for cheque/bank-transfer).</summary>
    [MaxLength(100)]
    public string? Bank { get; set; }

    /// <summary>Cheque or demand-draft number (optional).</summary>
    [MaxLength(60)]
    public string? DsNo { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
