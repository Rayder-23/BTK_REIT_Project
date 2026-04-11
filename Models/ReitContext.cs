using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace REIT_Project.Models;

public partial class ReitContext : DbContext
{
    public ReitContext()
    {
    }

    public ReitContext(DbContextOptions<ReitContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AdminUser> AdminUsers { get; set; }

    public virtual DbSet<Configuration> Configurations { get; set; }

    public virtual DbSet<Dividend> Dividends { get; set; }

    public virtual DbSet<Expense> Expenses { get; set; }

    public virtual DbSet<FundDetail> FundDetails { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<RentalIncome> RentalIncomes { get; set; }

    public virtual DbSet<Shareholder> Shareholders { get; set; }

    public virtual DbSet<Shbkaccount> Shbkaccounts { get; set; }

    public virtual DbSet<Transfer> Transfers { get; set; }

    public virtual DbSet<TrustFund> TrustFunds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.HasIndex(e => e.UserName, "UQ_Admin_userName").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creation_date");
            entity.Property(e => e.LastAction)
                .HasColumnType("datetime")
                .HasColumnName("last_action");
            entity.Property(e => e.LastLogin)
                .HasColumnType("datetime")
                .HasColumnName("last_login");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.SecurityLevel)
                .HasDefaultValue(1)
                .HasColumnName("security_level");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userName");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.InverseCreatedByNavigation)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_AdminUsers_CreatedBy");
        });

        modelBuilder.Entity<Configuration>(entity =>
        {
            entity.HasKey(e => e.ConfigId);

            entity.HasIndex(e => e.Key, "UQ_Configurations_key").IsUnique();

            entity.Property(e => e.ConfigId).HasColumnName("config_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Key)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("key");
            entity.Property(e => e.LastEdited)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("last_edited");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Value).HasColumnName("value");

            entity.HasOne(d => d.User).WithMany(p => p.Configurations)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Configurations_Admin");
        });

        modelBuilder.Entity<Dividend>(entity =>
        {
            entity.HasKey(e => e.DivId);

            entity.ToTable("Dividend");

            entity.HasIndex(e => new { e.ShId, e.FundId, e.Month, e.Year, e.DivType }, "UQ_Dividend_period").IsUnique();

            entity.Property(e => e.DivId).HasColumnName("div_id");
            entity.Property(e => e.AccountId).HasColumnName("account_id");
            entity.Property(e => e.Deduction)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("deduction");
            entity.Property(e => e.DivType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("regular")
                .HasColumnName("div_type");
            entity.Property(e => e.FundDtId).HasColumnName("fund_dt_id");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.GrossDivAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("gross_div_amount");
            entity.Property(e => e.Month)
                .HasDefaultValue(1)
                .HasColumnName("month");
            entity.Property(e => e.NetAmountPaid)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("net_amount_paid");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaidOn).HasColumnName("paid_on");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("payment_method");
            entity.Property(e => e.ShId).HasColumnName("sh_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Tax)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("tax");
            entity.Property(e => e.Year)
                .HasDefaultValue(2026)
                .HasColumnName("year");

            entity.HasOne(d => d.Account).WithMany(p => p.Dividends)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dividend_SHBKAccounts");

            entity.HasOne(d => d.FundDt).WithMany(p => p.Dividends)
                .HasForeignKey(d => d.FundDtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dividend_FundDetails");

            entity.HasOne(d => d.Fund).WithMany(p => p.Dividends)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dividend_TrustFund");

            entity.HasOne(d => d.Sh).WithMany(p => p.Dividends)
                .HasForeignKey(d => d.ShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Dividend_Shareholder");
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.Property(e => e.ExpenseId).HasColumnName("expense_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creation_date");
            entity.Property(e => e.Description)
                .HasMaxLength(200)
                .HasColumnName("description");
            entity.Property(e => e.ExpenseType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("expense_type");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.Month).HasColumnName("month");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaidBy).HasColumnName("paid_by");
            entity.Property(e => e.PaidOn).HasColumnName("paid_on");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Year).HasColumnName("year");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Expenses_ApprovedBy");

            entity.HasOne(d => d.Fund).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Expenses_TrustFund");

            entity.HasOne(d => d.PaidByNavigation).WithMany(p => p.Expenses)
                .HasForeignKey(d => d.PaidBy)
                .HasConstraintName("FK_Expenses_PaidBy");
        });

        modelBuilder.Entity<FundDetail>(entity =>
        {
            entity.HasKey(e => e.FundDtId);

            entity.Property(e => e.FundDtId).HasColumnName("fund_dt_id");
            entity.Property(e => e.AcquiredDate).HasColumnName("acquired_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PctOwned)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("pct_owned");
            entity.Property(e => e.ShId).HasColumnName("sh_id");
            entity.Property(e => e.ShareValue)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("share_value");

            entity.HasOne(d => d.Fund).WithMany(p => p.FundDetails)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FundDetails_TrustFund");

            entity.HasOne(d => d.Sh).WithMany(p => p.FundDetails)
                .HasForeignKey(d => d.ShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FundDetails_Shareholder");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.ActionDetails).HasColumnName("action_details");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("creation_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.OldInfo).HasColumnName("old_info");
            entity.Property(e => e.RecordId).HasColumnName("record_id");
            entity.Property(e => e.TableName)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("table_name");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Logs_AdminUsers");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AdditionalPayments)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("additional_payments");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("amount_paid");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Bank)
                .HasMaxLength(100)
                .HasColumnName("bank");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creation_date");
            entity.Property(e => e.DsNo)
                .HasMaxLength(60)
                .IsUnicode(false)
                .HasColumnName("ds_no");
            entity.Property(e => e.FundDtId).HasColumnName("fund_dt_id");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.GrossFundAmount)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("gross_fund_amount");
            entity.Property(e => e.NetAmountDue)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("net_amount_due");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("payment_type");
            entity.Property(e => e.ShId).HasColumnName("sh_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.Tax)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("tax");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Payments_AdminUsers");

            entity.HasOne(d => d.FundDt).WithMany(p => p.Payments)
                .HasForeignKey(d => d.FundDtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_FundDetails");

            entity.HasOne(d => d.Fund).WithMany(p => p.Payments)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_TrustFund");

            entity.HasOne(d => d.Sh).WithMany(p => p.Payments)
                .HasForeignKey(d => d.ShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Shareholder");
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasKey(e => e.PropId);

            entity.ToTable("Property");

            entity.Property(e => e.PropId).HasColumnName("prop_id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(60)
                .HasColumnName("city");
            entity.Property(e => e.Country)
                .HasMaxLength(60)
                .HasColumnName("country");
            entity.Property(e => e.CurrentValue)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("current_value");
            entity.Property(e => e.DateAdded).HasColumnName("date_added");
            entity.Property(e => e.DateRemoved).HasColumnName("date_removed");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PropName)
                .HasMaxLength(100)
                .HasColumnName("prop_name");
            entity.Property(e => e.PropType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("prop_type");
            entity.Property(e => e.ProvinceState)
                .HasMaxLength(60)
                .HasColumnName("province_state");
            entity.Property(e => e.PurchasePrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("purchase_price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
        });

        modelBuilder.Entity<RentalIncome>(entity =>
        {
            entity.HasKey(e => e.RentId);

            entity.ToTable("RentalIncome");

            entity.HasIndex(e => new { e.FundId, e.RentMonth, e.RentYear }, "UQ_RentalIncome_period").IsUnique();

            entity.Property(e => e.RentId).HasColumnName("rent_id");
            entity.Property(e => e.AmountDue)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount_due");
            entity.Property(e => e.AmountPaid)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount_paid");
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.LateFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("late_fee");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.RentMonth).HasColumnName("rent_month");
            entity.Property(e => e.RentYear).HasColumnName("rent_year");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("overdue")
                .HasColumnName("status");

            entity.HasOne(d => d.Fund).WithMany(p => p.RentalIncomes)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_RentalIncome_TrustFund");
        });

        modelBuilder.Entity<Shareholder>(entity =>
        {
            entity.HasKey(e => e.ShId);

            entity.ToTable("Shareholder");

            entity.HasIndex(e => e.UserName, "UQ_SH_userName").IsUnique();

            entity.HasIndex(e => e.Cnic, "UX_Shareholder_CNIC")
                .IsUnique()
                .HasFilter("([CNIC] IS NOT NULL)");

            entity.Property(e => e.ShId).HasColumnName("sh_id");
            entity.Property(e => e.Cnic)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("CNIC");
            entity.Property(e => e.ContactEmail)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("contactEmail");
            entity.Property(e => e.ContactNo)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("contactNo");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creationDate");
            entity.Property(e => e.FullName)
                .HasMaxLength(120)
                .HasColumnName("fullName");
            entity.Property(e => e.IsFiller).HasColumnName("is_filler");
            entity.Property(e => e.IsOverseas).HasColumnName("is_overseas");
            entity.Property(e => e.IsReit).HasColumnName("is_reit");
            entity.Property(e => e.NtnNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("NTN_no");
            entity.Property(e => e.PassportNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("passport_no");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.ShType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("sh_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.UserName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("userName");
        });

        modelBuilder.Entity<Shbkaccount>(entity =>
        {
            entity.HasKey(e => e.ShAccountId);

            entity.ToTable("SHBKAccounts");

            entity.HasIndex(e => new { e.ShId, e.AcNo }, "UQ_SHBKAccounts_acNo").IsUnique();

            entity.Property(e => e.ShAccountId).HasColumnName("sh_account_id");
            entity.Property(e => e.AcNo)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("acNo");
            entity.Property(e => e.AccountTitle)
                .HasMaxLength(120)
                .HasColumnName("account_title");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Bank)
                .HasMaxLength(100)
                .HasColumnName("bank");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creation_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.ShId).HasColumnName("sh_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Shbkaccounts)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_SHBKAccounts_AdminUsers");

            entity.HasOne(d => d.Sh).WithMany(p => p.Shbkaccounts)
                .HasForeignKey(d => d.ShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SHBKAccounts_Shareholder");
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.Property(e => e.TransferId).HasColumnName("transfer_id");
            entity.Property(e => e.AgreedPrice)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("agreed_price");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.FromShId).HasColumnName("from_sh_id");
            entity.Property(e => e.FundDtId).HasColumnName("fund_dt_id");
            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.InitiatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("initiated_date");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PctTransfer)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("pct_transfer");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.ToShId).HasColumnName("to_sh_id");
            entity.Property(e => e.TransferDate).HasColumnName("transfer_date");
            entity.Property(e => e.TransferType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("transfer_type");

            entity.HasOne(d => d.ApprovedByNavigation).WithMany(p => p.Transfers)
                .HasForeignKey(d => d.ApprovedBy)
                .HasConstraintName("FK_Transfers_AdminUsers");

            entity.HasOne(d => d.FromSh).WithMany(p => p.TransferFromShes)
                .HasForeignKey(d => d.FromShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfers_FromShareholder");

            entity.HasOne(d => d.FundDt).WithMany(p => p.Transfers)
                .HasForeignKey(d => d.FundDtId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfers_FundDetails");

            entity.HasOne(d => d.Fund).WithMany(p => p.Transfers)
                .HasForeignKey(d => d.FundId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfers_TrustFund");

            entity.HasOne(d => d.ToSh).WithMany(p => p.TransferToShes)
                .HasForeignKey(d => d.ToShId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Transfers_ToShareholder");
        });

        modelBuilder.Entity<TrustFund>(entity =>
        {
            entity.HasKey(e => e.FundId);

            entity.ToTable("TrustFund");

            entity.HasIndex(e => e.PropId, "UQ_TrustFund_prop").IsUnique();

            entity.Property(e => e.FundId).HasColumnName("fund_id");
            entity.Property(e => e.CreationDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnName("creationDate");
            entity.Property(e => e.FundTitle).HasMaxLength(500);
            entity.Property(e => e.FundTitle1)
                .HasMaxLength(100)
                .HasColumnName("fund_title");
            entity.Property(e => e.FundTotalValue)
                .HasColumnType("decimal(15, 2)")
                .HasColumnName("fund_total_value");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.PropId).HasColumnName("prop_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");

            entity.HasOne(d => d.Prop).WithOne(p => p.TrustFund)
                .HasForeignKey<TrustFund>(d => d.PropId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TrustFund_Property");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
