namespace REIT_Project.DTOs;

/// <summary>
/// Returned by GET /api/Fund/{id}/summary — consolidated fund health view.
/// </summary>
public class FundSummaryDto
{
    public int     FundId         { get; set; }
    public string  FundTitle      { get; set; } = null!;
    public decimal FundTotalValue { get; set; }

    public List<FundOwnerDto>      ActiveOwners   { get; set; } = [];
    public FundRentalSummaryDto?   LatestRental   { get; set; }
    public FundExpenseSummaryDto   Expenses       { get; set; } = null!;
}

public class FundOwnerDto
{
    public int     ShId         { get; set; }
    public string  FullName     { get; set; } = null!;
    public decimal PctOwned     { get; set; }
    public decimal ShareValue   { get; set; }
    public DateOnly AcquiredDate { get; set; }
}

public class FundRentalSummaryDto
{
    public int      RentId      { get; set; }
    public int      RentMonth   { get; set; }
    public int      RentYear    { get; set; }
    public decimal  AmountDue   { get; set; }
    public decimal? AmountPaid  { get; set; }
    public string   Status      { get; set; } = null!;
}

public class FundExpenseSummaryDto
{
    public int     Year          { get; set; }
    public decimal TotalPaid     { get; set; }
    public decimal TotalPending  { get; set; }
}
