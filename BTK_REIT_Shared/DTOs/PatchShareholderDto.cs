using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// Payload for PATCH /api/shareholders/{id}.
/// Only non-null fields are applied; null fields are left unchanged.
/// Bool flags use nullable bool so that omitting them leaves the DB untouched.
/// </summary>
public class PatchShareholderDto
{
    [Required]
    public int UserId { get; set; }

    [MaxLength(120)]
    public string? FullName { get; set; }

    [MaxLength(50)]
    public string? UserName { get; set; }

    [MaxLength(20)]
    public string? ShType { get; set; }

    [MaxLength(20)]
    public string? ContactNo { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    [MaxLength(20)]
    public string? Cnic { get; set; }

    [MaxLength(30)]
    public string? NtnNo { get; set; }

    [MaxLength(30)]
    public string? PassportNo { get; set; }

    public bool? IsFiller { get; set; }

    public bool? IsOverseas { get; set; }

    /// <summary>'active' | 'suspended' | 'inactive'</summary>
    [MaxLength(20)]
    public string? Status { get; set; }
}
