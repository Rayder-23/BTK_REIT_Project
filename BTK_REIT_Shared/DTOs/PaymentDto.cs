namespace BTK_REIT_Shared.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int ShId { get; set; }
    public int FundId { get; set; }
    public int FundDtId { get; set; }
    public decimal GrossFundAmount { get; set; }
    public decimal Tax { get; set; }
    public decimal AdditionalPayments { get; set; }
    public decimal NetAmountDue { get; set; }
    public decimal AmountPaid { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? PaymentType { get; set; }
    public string? Bank { get; set; }
    public string? DsNo { get; set; }
    public string Status { get; set; } = null!;
    public int? ApprovedBy { get; set; }
    public DateOnly CreationDate { get; set; }
    public string? Notes { get; set; }
}
