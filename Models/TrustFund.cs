using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class TrustFund
{
    public int FundId { get; set; }

    public int PropId { get; set; }

    public decimal FundTotalValue { get; set; }

    public string? Notes { get; set; }

    public DateTime? StartDate { get; set; }

    public string? FundTitle { get; set; }

    public string? FundTitle1 { get; set; }

    public DateOnly CreationDate { get; set; }

    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<FundDetail> FundDetails { get; set; } = new List<FundDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Property Prop { get; set; } = null!;

    public virtual ICollection<RentalIncome> RentalIncomes { get; set; } = new List<RentalIncome>();

    public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
