using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Used by PATCH /api/Rental/receive-payment/{id} to record rent arrival.
/// </summary>
public class RentPaymentDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "amount_paid must be greater than zero.")]
    public decimal AmountPaid { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "late_fee cannot be negative.")]
    public decimal LateFee { get; set; } = 0m;

    [Required]
    public DateOnly PaymentDate { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
