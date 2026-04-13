namespace BTK_REIT_Shared.DTOs;

public class ExpenseDto
{
    public int ExpenseId { get; set; }
    public int FundId { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public string ExpenseType { get; set; } = null!;
    public string Description { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateOnly? PaidOn { get; set; }
    public int? PaidBy { get; set; }
    public string Status { get; set; } = null!;
    public int? ApprovedBy { get; set; }
    public DateOnly CreationDate { get; set; }
    public string? Notes { get; set; }
}
