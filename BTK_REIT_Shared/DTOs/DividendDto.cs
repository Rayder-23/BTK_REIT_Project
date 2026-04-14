namespace BTK_REIT_Shared.DTOs;

public class DividendDto
{
    public int DivId { get; set; }
    public string DivType { get; set; } = null!;
    public int ShId { get; set; }
    /// <summary>Populated by GET endpoints (joined from Shareholder).</summary>
    public string? ShareholderName { get; set; }
    public int FundId { get; set; }
    /// <summary>Populated by GET endpoints (joined from TrustFund).</summary>
    public string? FundTitle { get; set; }
    public int FundDtId { get; set; }
    public int AccountId { get; set; }
    public decimal GrossDivAmount { get; set; }
    public decimal Tax { get; set; }
    public decimal Deduction { get; set; }
    public decimal NetAmountPaid { get; set; }
    public DateOnly? PaidOn { get; set; }
    public string? PaymentMethod { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
}
