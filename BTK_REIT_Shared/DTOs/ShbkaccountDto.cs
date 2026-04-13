namespace BTK_REIT_Shared.DTOs;

public class ShbkaccountDto
{
    public int ShAccountId { get; set; }
    public int ShId { get; set; }
    public string Bank { get; set; } = null!;
    public string AccountTitle { get; set; } = null!;
    public string AcNo { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int? ApprovedBy { get; set; }
    public DateOnly CreationDate { get; set; }
    public string? Notes { get; set; }
}
