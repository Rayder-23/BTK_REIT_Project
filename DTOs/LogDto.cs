namespace REIT_Project.DTOs;

public class LogDto
{
    public int LogId { get; set; }
    public int UserId { get; set; }
    public string TableName { get; set; } = null!;
    public int RecordId { get; set; }
    public string ActionDetails { get; set; } = null!;
    public string? OldInfo { get; set; }
    public DateTime CreationDate { get; set; }
    public string? Notes { get; set; }
}
