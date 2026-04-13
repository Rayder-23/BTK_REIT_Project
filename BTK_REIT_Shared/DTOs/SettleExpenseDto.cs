using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>Used by PATCH /api/Expense/settle/{id}.</summary>
public class SettleExpenseDto
{
    /// <summary>Admin user settling the expense — required for audit log.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Date the expense was actually paid.
    /// Defaults to today if omitted.
    /// </summary>
    public DateOnly? PaidOn { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
