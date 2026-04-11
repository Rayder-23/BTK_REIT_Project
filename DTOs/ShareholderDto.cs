namespace REIT_Project.DTOs;

public class ShareholderDto
{
    public int ShId { get; set; }
    public string ShType { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Password { get; set; }
    public string FullName { get; set; } = null!;
    public string? Cnic { get; set; }
    public string? NtnNo { get; set; }
    public string? PassportNo { get; set; }
    public string ContactNo { get; set; } = null!;
    public string ContactEmail { get; set; } = null!;
    public bool IsFiller { get; set; }
    public bool IsOverseas { get; set; }
    public bool IsReit { get; set; }
    public DateOnly CreationDate { get; set; }
    public string Status { get; set; } = null!;
}
