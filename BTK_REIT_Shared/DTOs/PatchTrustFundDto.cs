using System.ComponentModel.DataAnnotations;

namespace BTK_REIT_Shared.DTOs;

public class PatchTrustFundDto
{
    [Required]
    public int UserId { get; set; }

    /// <summary>Long/legal name — maps to the C# FundTitle property (no HasColumnName, stored as FundTitle in DB).</summary>
    public string? FundTitle  { get; set; }

    /// <summary>Short/identifier name — maps to C# FundTitle1 property (HasColumnName("fund_title") in DB).</summary>
    public string? FundTitle1 { get; set; }

    public string?  Status         { get; set; }
    public decimal? FundTotalValue { get; set; }
    public string?  Notes          { get; set; }
}
