namespace BTK_REIT_Shared.DTOs;

public class RentalIncomeDto
{
    public int RentId { get; set; }
    public int FundId { get; set; }
    /// <summary>Populated by GET endpoints (joined from TrustFund).</summary>
    public string? FundTitle { get; set; }
    public int RentMonth { get; set; }
    public int RentYear { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal LateFee { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}
