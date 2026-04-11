using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class Shareholder
{
    public int ShId { get; set; }

    public string ShType { get; set; } = null!;

    public string UserName { get; set; } = null!;

    public string? Password { get; set; }

    public string FullName { get; set; } = null!;

    public string? Cnic { get; set; }

    public string? NtnNo { get; set; }

    public string? PassportNo { get; set; }

    public string ContactNo { get; set; } = null!;

    public string ContactEmail { get; set; } = null!;

    public bool IsFiller { get; set; }

    public bool IsOverseas { get; set; }

    public bool IsReit { get; set; }

    public DateOnly CreationDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<FundDetail> FundDetails { get; set; } = new List<FundDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Shbkaccount> Shbkaccounts { get; set; } = new List<Shbkaccount>();

    public virtual ICollection<Transfer> TransferFromShes { get; set; } = new List<Transfer>();

    public virtual ICollection<Transfer> TransferToShes { get; set; } = new List<Transfer>();
}
