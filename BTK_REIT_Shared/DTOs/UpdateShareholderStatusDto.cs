using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Used by PATCH /api/Shareholder/{id}/status.
/// </summary>
public class UpdateShareholderStatusDto
{
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = null!;

    [Required]
    public int UserId { get; set; }
}
