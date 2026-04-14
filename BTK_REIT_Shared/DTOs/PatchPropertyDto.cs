using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Payload for PATCH /api/properties/{id}.
/// Only non-null fields are applied; null fields are left unchanged.
/// </summary>
public class PatchPropertyDto
{
    [Required]
    public int UserId { get; set; }

    [MaxLength(100)]
    public string? PropName { get; set; }

    /// <summary>'residential' | 'commercial' | 'mixed-use'</summary>
    [MaxLength(20)]
    public string? PropType { get; set; }

    [MaxLength(200)]
    public string? Address { get; set; }

    [MaxLength(60)]
    public string? City { get; set; }

    [MaxLength(60)]
    public string? ProvinceState { get; set; }

    [MaxLength(60)]
    public string? Country { get; set; }

    public DateOnly? DateAdded { get; set; }

    public DateOnly? DateRemoved { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "purchase_price must be greater than 0.")]
    public decimal? PurchasePrice { get; set; }

    public decimal? CurrentValue { get; set; }

    /// <summary>'active' | 'sold' | 'under-review'</summary>
    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
