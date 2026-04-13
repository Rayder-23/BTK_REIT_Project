using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Used by POST /api/Dividend/calculate to prepare dividend rows for a fund/period.
/// </summary>
public class DividendCalculateDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int FundId { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "rent_month must be between 1 and 12.")]
    public int RentMonth { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "rent_year must be between 2000 and 2100.")]
    public int RentYear { get; set; }

    /// <summary>
    /// Withholding tax rate expressed as a fraction (e.g. 0.15 = 15%).
    /// Defaults to 0.00 — pass the applicable rate explicitly.
    /// </summary>
    [Range(0, 1, ErrorMessage = "tax_rate must be between 0 and 1.")]
    public decimal TaxRate { get; set; } = 0m;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
