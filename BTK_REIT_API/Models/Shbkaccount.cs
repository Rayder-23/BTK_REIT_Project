using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class Shbkaccount
{
    public int ShAccountId { get; set; }

    public int ShId { get; set; }

    public string Bank { get; set; } = null!;

    public string AccountTitle { get; set; } = null!;

    public string AcNo { get; set; } = null!;

    public string Status { get; set; } = null!;

    public int? ApprovedBy { get; set; }

    public DateOnly CreationDate { get; set; }

    public string? Notes { get; set; }

    public virtual AdminUser? ApprovedByNavigation { get; set; }

    public virtual ICollection<Dividend> Dividends { get; set; } = new List<Dividend>();

    public virtual Shareholder Sh { get; set; } = null!;
}
