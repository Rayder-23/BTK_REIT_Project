namespace BTK_REIT_Shared.DTOs;

public class AdminUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = null!;
    public int SecurityLevel { get; set; }
    public string Password { get; set; } = null!;
    public int? CreatedBy { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime? LastAction { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly CreationDate { get; set; }
    public string? Notes { get; set; }
}
