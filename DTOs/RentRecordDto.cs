using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>
/// Used by POST /api/Rental/record to create the initial RentalIncome entry.
/// </summary>
public class RentRecordDto
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

    [Required]
    public DateOnly DueDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "amount_due must be greater than zero.")]
    public decimal AmountDue { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
