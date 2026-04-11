using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

public class TransferInitiateDto
{
    [Required]
    public int UserId { get; set; }         // Admin initiating; maps to approved_by

    [Required]
    public int FundId { get; set; }

    [Required]
    public int FromShId { get; set; }       // Seller

    [Required]
    public int ToShId { get; set; }         // Buyer

    /// <summary>Must be 'sale' or 'gift'.</summary>
    [Required]
    [MaxLength(20)]
    public string TransferType { get; set; } = null!;

    /// <summary>Percentage of fund ownership being transferred. Must be > 0 and <= 100.</summary>
    [Required]
    [Range(0.01, 100.00, ErrorMessage = "pct_transfer must be between 0.01 and 100.00.")]
    public decimal PctTransfer { get; set; }

    /// <summary>Required when TransferType is 'sale'.</summary>
    public decimal? AgreedPrice { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
