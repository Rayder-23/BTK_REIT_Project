using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class Dividend
{
    public int DivId { get; set; }

    public string DivType { get; set; } = null!;

    public int ShId { get; set; }

    public int FundId { get; set; }

    public int FundDtId { get; set; }

    public int AccountId { get; set; }

    public decimal GrossDivAmount { get; set; }

    public decimal Tax { get; set; }

    public decimal Deduction { get; set; }

    public decimal NetAmountPaid { get; set; }

    public DateOnly? PaidOn { get; set; }

    public string? PaymentMethod { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public int Month { get; set; }

    public int Year { get; set; }

    public virtual Shbkaccount Account { get; set; } = null!;

    public virtual TrustFund Fund { get; set; } = null!;

    public virtual FundDetail FundDt { get; set; } = null!;

    public virtual Shareholder Sh { get; set; } = null!;
}
