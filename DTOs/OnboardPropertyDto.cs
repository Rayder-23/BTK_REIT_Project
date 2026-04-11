using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

public class OnboardPropertyDto
{
    // ── Admin context ────────────────────────────────────────────────────────
    [Required]
    public int UserId { get; set; }

    // ── Property fields ──────────────────────────────────────────────────────

    /// <summary>Must be 'residential', 'commercial', or 'mixed-use'.</summary>
    [Required]
    [MaxLength(20)]
    public string PropType { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string PropName { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = null!;

    [Required]
    [MaxLength(60)]
    public string City { get; set; } = null!;

    [MaxLength(60)]
    public string? ProvinceState { get; set; }

    [Required]
    [MaxLength(60)]
    public string Country { get; set; } = null!;

    /// <summary>Date the property was acquired. Defaults to today if omitted.</summary>
    public DateOnly? DateAdded { get; set; }

    /// <summary>Optional disposal date. Must be later than DateAdded when provided.</summary>
    public DateOnly? DateRemoved { get; set; }

    /// <summary>Must be greater than zero.</summary>
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "purchase_price must be greater than 0.")]
    public decimal PurchasePrice { get; set; }

    public decimal? CurrentValue { get; set; }

    /// <summary>Must be 'active', 'sold', or 'under-review'. Defaults to 'active'.</summary>
    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(500)]
    public string? PropNotes { get; set; }

    // ── TrustFund fields ─────────────────────────────────────────────────────

    /// <summary>Display title shown in reports (maps to fund_title, 100 chars).</summary>
    [MaxLength(100)]
    public string? FundTitle { get; set; }

    /// <summary>
    /// Long-form fund title (maps to FundTitle, 500 chars).
    /// Defaults to "[PropName] Trust Fund" if omitted.
    /// </summary>
    [MaxLength(500)]
    public string? FundTitleLong { get; set; }

    /// <summary>
    /// Override the fund's total value. Defaults to PurchasePrice when omitted.
    /// </summary>
    [Range(0.01, double.MaxValue, ErrorMessage = "fund_total_value must be greater than 0 if provided.")]
    public decimal? FundTotalValue { get; set; }

    /// <summary>Fund operational start date. Defaults to DateAdded when omitted.</summary>
    public DateTime? FundStartDate { get; set; }

    [MaxLength(500)]
    public string? FundNotes { get; set; }
}
