using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>Used by POST /api/Expense/record.</summary>
public class CreateExpenseDto
{
    /// <summary>Admin user creating the record — required for audit log.</summary>
    [Required]
    public int UserId { get; set; }

    [Required]
    public int FundId { get; set; }

    [Required]
    [Range(1, 12, ErrorMessage = "month must be between 1 and 12.")]
    public int Month { get; set; }

    [Required]
    [Range(2000, 2100, ErrorMessage = "year must be between 2000 and 2100.")]
    public int Year { get; set; }

    /// <summary>Must be one of: 'maintenance', 'utility', 'tax', 'insurance', 'mgmt-fee', 'other'.</summary>
    [Required]
    [MaxLength(20)]
    public string ExpenseType { get; set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Description { get; set; } = null!;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "amount must be greater than zero.")]
    public decimal Amount { get; set; }

    /// <summary>Optional: Shareholder who will pay / has paid this expense.</summary>
    public int? PaidBy { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
