using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class Transfer
{
    public int TransferId { get; set; }

    public string TransferType { get; set; } = null!;

    public int? ApprovedBy { get; set; }

    public int FundId { get; set; }

    public int FundDtId { get; set; }

    public int FromShId { get; set; }

    public int ToShId { get; set; }

    public decimal PctTransfer { get; set; }

    public decimal? AgreedPrice { get; set; }

    public DateOnly InitiatedDate { get; set; }

    public DateOnly? TransferDate { get; set; }

    public string Status { get; set; } = null!;

    public string? Notes { get; set; }

    public virtual AdminUser? ApprovedByNavigation { get; set; }

    public virtual Shareholder FromSh { get; set; } = null!;

    public virtual TrustFund Fund { get; set; } = null!;

    public virtual FundDetail FundDt { get; set; } = null!;

    public virtual Shareholder ToSh { get; set; } = null!;
}
