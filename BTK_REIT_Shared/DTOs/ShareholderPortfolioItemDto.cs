namespace BTK_REIT_Shared.DTOs;

/// <summary>
/// One row in the portfolio response for GET /api/Shareholder/{id}/portfolio.
/// </summary>
public class ShareholderPortfolioItemDto
{
    public int      FundId       { get; set; }
    public string   FundTitle    { get; set; } = null!;
    public decimal  PctOwned     { get; set; }
    public decimal  ShareValue   { get; set; }
    public DateOnly AcquiredDate { get; set; }
}
