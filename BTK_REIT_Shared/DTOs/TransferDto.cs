namespace BTK_REIT_Shared.DTOs;

public class TransferDto
{
    public int TransferId { get; set; }
    public string TransferType { get; set; } = null!;
    public int? ApprovedBy { get; set; }
    public int FundId { get; set; }
    public int FundDtId { get; set; }
    public int FromShId { get; set; }
    public int ToShId { get; set; }
    public decimal PctTransfer { get; set; }
    public decimal? AgreedPrice { get; set; }
    public DateOnly InitiatedDate { get; set; }
    public DateOnly? TransferDate { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }

    // Resolved display fields — populated by GET /api/transfers
    public string? FromShName { get; set; }
    public string? ToShName { get; set; }
    public string? FundTitle { get; set; }
}
