using System;
using System.Collections.Generic;

namespace REIT_Project.Models;

public partial class AdminUser
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public int SecurityLevel { get; set; }

    public string Password { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime? LastLogin { get; set; }

    public DateTime? LastAction { get; set; }

    public string Status { get; set; } = null!;

    public DateOnly CreationDate { get; set; }

    public string? Notes { get; set; }

    public virtual ICollection<Configuration> Configurations { get; set; } = new List<Configuration>();

    public virtual AdminUser? CreatedByNavigation { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();

    public virtual ICollection<AdminUser> InverseCreatedByNavigation { get; set; } = new List<AdminUser>();

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Shbkaccount> Shbkaccounts { get; set; } = new List<Shbkaccount>();

    public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();
}
