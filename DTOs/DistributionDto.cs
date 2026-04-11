namespace REIT_Project.DTOs
{
    public class DistributionRequest
    {
        public int RentId { get; set; }
        public decimal TaxRate { get; set; } = 0.15m; // Standard withholding
        public int UserId { get; set; }               // Admin performing the distribution
    }
}