using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

public class BankAccountCreateDto
{
    // --- Required fields ---
    [Required]
    public int ShId { get; set; }

    [Required]
    public int UserId { get; set; }   // Admin performing the action → maps to approved_by

    [Required]
    [MaxLength(100)]
    public string Bank { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string AccountTitle { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    public string AcNo { get; set; } = null!;

    // Optional — defaults to 'active' in controller if omitted
    [MaxLength(20)]
    public string? Status { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
