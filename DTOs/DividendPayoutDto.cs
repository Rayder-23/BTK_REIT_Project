using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>
/// Used by POST /api/Dividend/confirm-payout/{id} to stamp the actual bank transfer.
/// </summary>
public class DividendPayoutDto
{
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Actual date the bank transfer was executed.
    /// Defaults to today if omitted.
    /// </summary>
    public DateOnly? PaidOn { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
