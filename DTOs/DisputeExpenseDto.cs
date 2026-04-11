using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>Used by PATCH /api/Expense/dispute/{id}.</summary>
public class DisputeExpenseDto
{
    /// <summary>Admin user raising the dispute — required for audit log.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>Reason for the dispute. Stored in Notes.</summary>
    [Required]
    [MaxLength(500)]
    public string Reason { get; set; } = null!;
}
