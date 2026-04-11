using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class RentalIncome
{
    public int RentId { get; set; }

    public int FundId { get; set; }

    public int RentMonth { get; set; }

    public int RentYear { get; set; }

    public DateOnly DueDate { get; set; }

    public decimal AmountDue { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal LateFee { get; set; }

    public DateOnly? PaymentDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual TrustFund Fund { get; set; } = null!;
}
