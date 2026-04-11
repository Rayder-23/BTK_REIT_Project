using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class FundDetail
{
    public int FundDtId { get; set; }

    public int FundId { get; set; }

    public int ShId { get; set; }

    public decimal PctOwned { get; set; }

    public decimal ShareValue { get; set; }

    public DateOnly AcquiredDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();

    public virtual TrustFund Fund { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Shareholder Sh { get; set; } = null!;

    public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
