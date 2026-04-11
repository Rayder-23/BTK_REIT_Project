using System.ComponentModel.DataAnnotations;

namespace REIT_Project.DTOs;

/// <summary>Used by POST /api/Config/set — upserts a configuration key/value pair.</summary>
public class ConfigSetDto
{
    /// <summary>Admin user performing the upsert — required for audit log.</summary>
    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    [Required]
    public string Value { get; set; } = null!;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
