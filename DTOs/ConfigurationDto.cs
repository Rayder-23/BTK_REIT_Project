namespace REIT_Project.DTOs;

public class ConfigurationDto
{
    public int ConfigId { get; set; }
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public bool IsActive { get; set; }
    public int? UserId { get; set; }
    public DateTime LastEdited { get; set; }
    public string? Notes { get; set; }
}
