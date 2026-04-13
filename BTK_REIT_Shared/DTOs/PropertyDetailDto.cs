namespace BTK_REIT_Shared.DTOs
{
    public class PropertyDetailDto
    {
        public string PropName { get; set; } = null!;
        public string FundTitle { get; set; } = null!;
        public decimal TotalValue { get; set; }
        public List<OwnerDto> CurrentOwners { get; set; } = new();
    }

    public class OwnerDto
    {
        public string FullName { get; set; } = null!;
        public decimal PctOwned { get; set; }
    }
}