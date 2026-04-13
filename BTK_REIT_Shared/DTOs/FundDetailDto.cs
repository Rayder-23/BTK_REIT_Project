namespace BTK_REIT_Shared.DTOs;

public class FundDetailDto
{
    public int FundDtId { get; set; }
    public int FundId { get; set; }
    public int ShId { get; set; }
    public decimal PctOwned { get; set; }
    public decimal ShareValue { get; set; }
    public DateOnly AcquiredDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Notes { get; set; }
}
