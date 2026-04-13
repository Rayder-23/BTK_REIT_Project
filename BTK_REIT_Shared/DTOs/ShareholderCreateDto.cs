using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

public class ShareholderCreateDto
{
    // --- Required fields ---
    [Required]
    public int UserId { get; set; }   // Admin performing the registration

    [Required]
    [MaxLength(50)]
    public string UserName { get; set; } = null!;

    [Required]
    [MaxLength(120)]
    public string FullName { get; set; } = null!;

    [Required]
    [MaxLength(20)]
    public string ContactNo { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string ContactEmail { get; set; } = null!;

    [Required]
    public string ShType { get; set; } = null!;   // 'individual' | 'company' | 'trust'

    // --- Optional fields ---
    [MaxLength(20)]
    public string? Cnic { get; set; }

    [MaxLength(30)]
    public string? NtnNo { get; set; }

    [MaxLength(30)]
    public string? PassportNo { get; set; }

    [MaxLength(255)]
    public string? Password { get; set; }

    // BIT flags — caller sends 0 or 1; defaults to false
    public bool IsFiller { get; set; } = false;
    public bool IsOverseas { get; set; } = false;
    public bool IsReit { get; set; } = false;
}
