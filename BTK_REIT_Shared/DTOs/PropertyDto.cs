namespace BTK_REIT_Shared.DTOs;

public class PropertyDto
{
    public int PropId { get; set; }
    public string PropType { get; set; } = null!;
    public string PropName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string City { get; set; } = null!;
    public string? ProvinceState { get; set; }
    public string Country { get; set; } = null!;
    public DateOnly DateAdded { get; set; }
    public DateOnly? DateRemoved { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal? CurrentValue { get; set; }
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}