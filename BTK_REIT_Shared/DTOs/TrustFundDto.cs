namespace BTK_REIT_Shared.DTOs;

public class TrustFundDto
{
    public int FundId { get; set; }
    public int PropId { get; set; }
    public decimal FundTotalValue { get; set; }
    public string? Notes { get; set; }
    public DateTime? StartDate { get; set; }
    public string? FundTitle { get; set; }
    public string? FundTitle1 { get; set; }
    public DateOnly CreationDate { get; set; }
}
