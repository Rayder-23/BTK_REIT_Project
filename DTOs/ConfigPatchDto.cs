using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>
/// Used by PATCH /api/Config/append/{key} and PATCH /api/Config/remove/{key}.
/// Carries the single CSV token to add or remove.
/// </summary>
public class ConfigPatchDto
{
    /// <summary>Admin user performing the change — required for the audit log.</summary>
    [Required]
    public int UserId { get; set; }

    /// <summary>The individual value to append or remove (e.g. "bank-transfer").</summary>
    [Required]
    [MaxLength(100)]
    public string Value { get; set; } = null!;
}
