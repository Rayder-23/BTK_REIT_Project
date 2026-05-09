-- =============================================================================
-- REIT Database Schema
-- =============================================================================
-- Description : Full schema definition for a Real Estate Investment Trust (REIT)
--               database management system. Covers shareholder management,
--               property ownership, fund operations, rental income, dividend
--               distribution, expense tracking, transfers, and audit logging.
--
-- Database    : SQL Server Express
-- Version     : 1.0
-- Created     : 2026-04-29
-- =============================================================================
-- TABLE OF CONTENTS
--   1.  Shareholder
--   2.  Property
--   3.  AdminUsers
--   4.  TrustFund
--   5.  FundDetails
--   6.  Payments
--   7.  RentalIncome
--   8.  SHBKAccounts
--   9.  Dividend
--   10. Transfers
--   11. Expenses
--   12. Logs
--   13. Configurations
--   14. Deferred FK  — FundDetails.transfer_id → Transfers
--   15. Filtered Unique Index — Shareholder.CNIC (NULL-safe)
-- =============================================================================


USE REIT;
GO


-- =============================================================================
-- 1. Shareholder
-- =============================================================================
-- Stores every person or entity that can hold a stake in a fund, including the
-- REIT fund itself (flagged with is_reit = 1). The REIT's mandatory 10%
-- ownership stake is represented as a shareholder row here and tracked in
-- FundDetails like any other stakeholder.
-- =============================================================================

CREATE TABLE Shareholder (
    sh_id        INT            NOT NULL IDENTITY(1,1),
    sh_type      VARCHAR(20)    NOT NULL,
    userName     VARCHAR(50)    NOT NULL,
    password     VARCHAR(255)       NULL,               -- NULL until dashboard login is implemented
    fullName     NVARCHAR(120)  NOT NULL,
    CNIC         VARCHAR(20)        NULL,               -- NULL for companies; unique enforced via filtered index below
    NTN_no       VARCHAR(30)        NULL,
    passport_no  VARCHAR(30)        NULL,               -- Required when is_overseas = 1
    contactNo    VARCHAR(20)    NOT NULL,
    contactEmail VARCHAR(100)   NOT NULL,
    is_filler    BIT            NOT NULL DEFAULT 0,
    is_overseas  BIT            NOT NULL DEFAULT 0,
    is_reit      BIT            NOT NULL DEFAULT 0,
    creationDate DATE           NOT NULL DEFAULT GETDATE(),
    status       VARCHAR(20)    NOT NULL DEFAULT 'active',

    CONSTRAINT PK_Shareholder   PRIMARY KEY (sh_id),
    CONSTRAINT UQ_SH_userName   UNIQUE      (userName),
    CONSTRAINT CHK_SH_type      CHECK       (sh_type IN ('individual', 'company', 'trust')),
    CONSTRAINT CHK_SH_status    CHECK       (status  IN ('active', 'suspended', 'exited')),
    CONSTRAINT CHK_SH_email     CHECK       (contactEmail LIKE '%@%.%')
);
GO


-- =============================================================================
-- 2. Property
-- =============================================================================
-- Stores each physical real estate asset held by the REIT. Each property maps
-- one-to-one with a TrustFund record. Most financial tables reference fund_id
-- rather than prop_id to keep the financial layer separate from the asset layer.
-- =============================================================================

CREATE TABLE Property (
    prop_id         INT            NOT NULL IDENTITY(1,1),
    prop_type       VARCHAR(20)    NOT NULL,
    prop_name       NVARCHAR(100)  NOT NULL,
    address         NVARCHAR(200)  NOT NULL,
    city            NVARCHAR(60)   NOT NULL,
    province_state  NVARCHAR(60)       NULL,
    country         NVARCHAR(60)   NOT NULL,
    date_added      DATE           NOT NULL,
    date_removed    DATE               NULL,             -- NULL if property is still active
    purchase_price  DECIMAL(15,2)  NOT NULL,
    current_value   DECIMAL(15,2)      NULL,
    status          VARCHAR(20)    NOT NULL DEFAULT 'active',
    notes           NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Property      PRIMARY KEY (prop_id),
    CONSTRAINT CHK_Prop_type    CHECK (prop_type IN ('residential', 'commercial', 'mixed-use')),
    CONSTRAINT CHK_Prop_status  CHECK (status    IN ('active', 'sold', 'under-review')),
    CONSTRAINT CHK_Prop_dates   CHECK (date_removed IS NULL OR date_removed > date_added),
    CONSTRAINT CHK_Prop_price   CHECK (purchase_price > 0),
    CONSTRAINT CHK_Prop_value   CHECK (current_value  IS NULL OR current_value > 0)
);
GO


-- =============================================================================
-- 3. AdminUsers
-- =============================================================================
-- Stores system administrators who manage operations — approving transfers,
-- verifying bank accounts, recording expenses, and logging actions. Separate
-- from Shareholder; a person can be both but they exist independently.
-- created_by is a self-referencing FK — the root admin has NULL here.
-- =============================================================================

CREATE TABLE AdminUsers (
    user_id        INT           NOT NULL IDENTITY(1,1),
    userName       VARCHAR(50)   NOT NULL,
    security_level INT           NOT NULL DEFAULT 1,
    password       VARCHAR(255)  NOT NULL,
    created_by     INT               NULL,              -- Self-referencing FK; NULL for root admin
    last_login     DATETIME          NULL,
    last_action    DATETIME          NULL,
    status         VARCHAR(20)   NOT NULL DEFAULT 'active',
    creation_date  DATE          NOT NULL DEFAULT GETDATE(),
    notes          NVARCHAR(MAX)     NULL,

    CONSTRAINT PK_AdminUsers            PRIMARY KEY (user_id),
    CONSTRAINT UQ_Admin_userName        UNIQUE      (userName),
    CONSTRAINT CHK_Admin_status         CHECK       (status         IN ('active', 'suspended', 'revoked')),
    CONSTRAINT CHK_Admin_sec_level      CHECK       (security_level >= 1),
    CONSTRAINT FK_AdminUsers_CreatedBy  FOREIGN KEY (created_by)
        REFERENCES AdminUsers(user_id)
);
GO


-- =============================================================================
-- 4. TrustFund
-- =============================================================================
-- Represents the investable fund associated with a property. One-to-one with
-- Property enforced by the unique constraint on prop_id. Most financial tables
-- reference fund_id rather than prop_id directly.
-- =============================================================================

CREATE TABLE TrustFund (
    fund_id          INT            NOT NULL IDENTITY(1,1),
    prop_id          INT            NOT NULL,
    fund_title       NVARCHAR(100)      NULL,
    fund_total_value DECIMAL(15,2)  NOT NULL,
    creationDate     DATE           NOT NULL DEFAULT GETDATE(),
    notes            NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_TrustFund             PRIMARY KEY (fund_id),
    CONSTRAINT FK_TrustFund_Property    FOREIGN KEY (prop_id)
        REFERENCES Property(prop_id),
    CONSTRAINT UQ_TrustFund_prop        UNIQUE      (prop_id),       -- One fund per property
    CONSTRAINT CHK_TrustFund_value      CHECK       (fund_total_value > 0)
);
GO


-- =============================================================================
-- 5. FundDetails
-- =============================================================================
-- Ownership ledger for each fund. One row per shareholder stake per fund.
-- Stakes are NEVER updated in place — when ownership changes, the existing row
-- receives an end_date and new rows are opened. This preserves a full audit
-- trail of who owned what percentage and when.
--
-- All active rows (end_date IS NULL) for a given fund_id must sum to 100%.
-- The REIT's mandatory 10% stake appears here as a row pointing to the
-- is_reit = 1 Shareholder record.
--
-- NOTE: transfer_id FK to Transfers is added after Transfers is created
--       (see Section 14 — deferred FK).
-- =============================================================================

CREATE TABLE FundDetails (
    fund_dt_id    INT            NOT NULL IDENTITY(1,1),
    fund_id       INT            NOT NULL,
    sh_id         INT            NOT NULL,
    pct_owned     DECIMAL(5,2)   NOT NULL,
    share_value   DECIMAL(15,2)  NOT NULL,
    acquired_date DATE           NOT NULL,
    end_date      DATE               NULL,              -- NULL if stake is still active
    transfer_id   INT                NULL,              -- FK added after Transfers is created
    notes         NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_FundDetails             PRIMARY KEY (fund_dt_id),
    CONSTRAINT FK_FundDetails_TrustFund   FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT FK_FundDetails_Shareholder FOREIGN KEY (sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT CHK_FundDetails_pct        CHECK (pct_owned   >  0 AND pct_owned <= 100),
    CONSTRAINT CHK_FundDetails_value      CHECK (share_value >  0),
    CONSTRAINT CHK_FundDetails_dates      CHECK (end_date IS NULL OR end_date > acquired_date)
);
GO


-- =============================================================================
-- 6. Payments
-- =============================================================================
-- Records financial settlements from shareholders when acquiring a stake,
-- typically triggered by a completed Transfer. Tracks gross amount, tax,
-- additional charges, and net amount due vs amount actually received.
-- ds_no = Deposit Slip number, used for bank reconciliation.
-- =============================================================================

CREATE TABLE Payments (
    payment_id          INT            NOT NULL IDENTITY(1,1),
    sh_id               INT            NOT NULL,
    fund_id             INT            NOT NULL,
    fund_dt_id          INT            NOT NULL,
    gross_fund_amount   DECIMAL(15,2)  NOT NULL,
    tax                 DECIMAL(12,2)  NOT NULL DEFAULT 0,
    additional_payments DECIMAL(12,2)  NOT NULL DEFAULT 0,
    net_amount_due      DECIMAL(15,2)  NOT NULL,
    amount_paid         DECIMAL(15,2)  NOT NULL DEFAULT 0,
    payment_date        DATE               NULL,
    payment_type        VARCHAR(30)        NULL,
    bank                NVARCHAR(100)      NULL,
    ds_no               VARCHAR(60)        NULL,
    status              VARCHAR(20)    NOT NULL DEFAULT 'pending',
    approved_by         INT                NULL,
    creation_date       DATE           NOT NULL DEFAULT GETDATE(),
    notes               NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Payments              PRIMARY KEY (payment_id),
    CONSTRAINT FK_Payments_Shareholder  FOREIGN KEY (sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT FK_Payments_TrustFund    FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT FK_Payments_FundDetails  FOREIGN KEY (fund_dt_id)
        REFERENCES FundDetails(fund_dt_id),
    CONSTRAINT FK_Payments_AdminUsers   FOREIGN KEY (approved_by)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT CHK_Payments_status      CHECK (status       IN ('pending', 'paid', 'partial', 'overdue')),
    CONSTRAINT CHK_Payments_type        CHECK (payment_type IN ('bank-transfer', 'cheque', 'cash')),
    CONSTRAINT CHK_Payments_gross       CHECK (gross_fund_amount   >  0),
    CONSTRAINT CHK_Payments_tax         CHECK (tax                 >= 0),
    CONSTRAINT CHK_Payments_additional  CHECK (additional_payments >= 0),
    CONSTRAINT CHK_Payments_net         CHECK (net_amount_due      >= 0),
    CONSTRAINT CHK_Payments_paid        CHECK (amount_paid         >= 0)
);
GO


-- =============================================================================
-- 7. RentalIncome
-- =============================================================================
-- Monthly rent record per fund. One row per fund per month/year period,
-- enforced by the composite unique constraint. Records what was owed, what was
-- received, any late fees applied, and the payment date for lateness tracking.
-- Status starts as 'overdue' on creation and is updated as payment arrives.
-- =============================================================================

CREATE TABLE RentalIncome (
    rent_id      INT            NOT NULL IDENTITY(1,1),
    fund_id      INT            NOT NULL,
    rent_month   INT            NOT NULL,
    rent_year    INT            NOT NULL,
    due_date     DATE           NOT NULL,
    amount_due   DECIMAL(12,2)  NOT NULL,
    amount_paid  DECIMAL(12,2)  NOT NULL DEFAULT 0,
    late_fee     DECIMAL(10,2)  NOT NULL DEFAULT 0,
    payment_date DATE               NULL,
    status       VARCHAR(20)    NOT NULL DEFAULT 'overdue',
    notes        NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_RentalIncome          PRIMARY KEY (rent_id),
    CONSTRAINT FK_RentalIncome_TrustFund FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT UQ_RentalIncome_period   UNIQUE (fund_id, rent_month, rent_year),  -- One record per fund per period
    CONSTRAINT CHK_Rent_month           CHECK  (rent_month  BETWEEN 1 AND 12),
    CONSTRAINT CHK_Rent_year            CHECK  (rent_year   BETWEEN 2000 AND 2100),
    CONSTRAINT CHK_Rent_status          CHECK  (status      IN ('paid', 'partial', 'overdue')),
    CONSTRAINT CHK_Rent_amount_due      CHECK  (amount_due  >  0),
    CONSTRAINT CHK_Rent_amount_paid     CHECK  (amount_paid >= 0),
    CONSTRAINT CHK_Rent_late_fee        CHECK  (late_fee    >= 0)
);
GO


-- =============================================================================
-- 8. SHBKAccounts
-- =============================================================================
-- Shareholder bank accounts. Each account must be admin-approved before it
-- can receive dividend payments. A shareholder can hold multiple accounts but
-- the same account number cannot be registered twice for the same shareholder.
-- =============================================================================

CREATE TABLE SHBKAccounts (
    sh_account_id INT            NOT NULL IDENTITY(1,1),
    sh_id         INT            NOT NULL,
    bank          NVARCHAR(100)  NOT NULL,
    account_title NVARCHAR(120)  NOT NULL,
    acNo          VARCHAR(30)    NOT NULL,
    status        VARCHAR(20)    NOT NULL DEFAULT 'active',
    approved_by   INT                NULL,
    creation_date DATE           NOT NULL DEFAULT GETDATE(),
    notes         NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_SHBKAccounts              PRIMARY KEY (sh_account_id),
    CONSTRAINT FK_SHBKAccounts_Shareholder  FOREIGN KEY (sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT FK_SHBKAccounts_AdminUsers   FOREIGN KEY (approved_by)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT UQ_SHBKAccounts_acNo         UNIQUE (sh_id, acNo),   -- Same account number cannot be registered twice per shareholder
    CONSTRAINT CHK_SHBK_status              CHECK  (status IN ('active', 'inactive', 'rejected'))
);
GO


-- =============================================================================
-- 9. Dividend
-- =============================================================================
-- Monthly payout record per shareholder per fund per period. Calculated from
-- RentalIncome.amount_paid proportional to FundDetails.pct_owned.
-- Financial amounts are stored as snapshots — gross, tax, deduction, and net
-- are locked at calculation time and do not change if source records are edited.
-- The REIT's own stake (is_reit = 1) does not receive a dividend row.
-- =============================================================================

CREATE TABLE Dividend (
    div_id           INT            NOT NULL IDENTITY(1,1),
    div_type         VARCHAR(20)    NOT NULL DEFAULT 'regular',
    sh_id            INT            NOT NULL,
    fund_id          INT            NOT NULL,
    fund_dt_id       INT            NOT NULL,
    account_id       INT            NOT NULL,
    month            INT            NOT NULL,
    year             INT            NOT NULL,
    gross_div_amount DECIMAL(12,2)  NOT NULL,
    tax              DECIMAL(12,2)  NOT NULL DEFAULT 0,
    deduction        DECIMAL(12,2)  NOT NULL DEFAULT 0,
    net_amount_paid  DECIMAL(12,2)  NOT NULL,
    paid_on          DATE               NULL,
    payment_method   VARCHAR(30)        NULL,
    status           VARCHAR(20)    NOT NULL DEFAULT 'pending',
    notes            NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Dividend              PRIMARY KEY (div_id),
    CONSTRAINT FK_Dividend_Shareholder  FOREIGN KEY (sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT FK_Dividend_TrustFund    FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT FK_Dividend_FundDetails  FOREIGN KEY (fund_dt_id)
        REFERENCES FundDetails(fund_dt_id),
    CONSTRAINT FK_Dividend_SHBKAccounts FOREIGN KEY (account_id)
        REFERENCES SHBKAccounts(sh_account_id),
    CONSTRAINT UQ_Dividend_period       UNIQUE (sh_id, fund_id, month, year, div_type),
    CONSTRAINT CHK_Div_type             CHECK  (div_type       IN ('regular', 'special')),
    CONSTRAINT CHK_Div_status           CHECK  (status         IN ('pending', 'paid', 'on-hold')),
    CONSTRAINT CHK_Div_method           CHECK  (payment_method IN ('bank-transfer', 'cheque', 'cash')),
    CONSTRAINT CHK_Div_gross            CHECK  (gross_div_amount >  0),
    CONSTRAINT CHK_Div_tax              CHECK  (tax             >= 0),
    CONSTRAINT CHK_Div_deduction        CHECK  (deduction       >= 0),
    CONSTRAINT CHK_Div_net              CHECK  (net_amount_paid >= 0),
    CONSTRAINT CHK_Div_month            CHECK  (month  BETWEEN 1 AND 12),
    CONSTRAINT CHK_Div_year             CHECK  (year   BETWEEN 2000 AND 2100)
);
GO


-- =============================================================================
-- 10. Transfers
-- =============================================================================
-- Records ownership stakes moving between shareholders. On status = 'completed':
--   1. Set end_date on the seller's FundDetails row
--   2. Open a new FundDetails row for the seller's reduced stake
--   3. Open a new FundDetails row for the buyer's acquired stake
-- The REIT must always retain a minimum of 10% — enforced at application level.
-- agreed_price is NULL for gifts and inheritances.
-- =============================================================================

CREATE TABLE Transfers (
    transfer_id    INT            NOT NULL IDENTITY(1,1),
    transfer_type  VARCHAR(20)    NOT NULL,
    approved_by    INT                NULL,
    fund_id        INT            NOT NULL,
    fund_dt_id     INT            NOT NULL,
    from_sh_id     INT            NOT NULL,
    to_sh_id       INT            NOT NULL,
    pct_transfer   DECIMAL(5,2)   NOT NULL,
    agreed_price   DECIMAL(15,2)      NULL,             -- NULL for gifts / inheritances
    initiated_date DATE           NOT NULL DEFAULT GETDATE(),
    transfer_date  DATE               NULL,             -- NULL until completed
    status         VARCHAR(20)    NOT NULL DEFAULT 'pending',
    notes          NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Transfers                 PRIMARY KEY (transfer_id),
    CONSTRAINT FK_Transfers_AdminUsers      FOREIGN KEY (approved_by)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT FK_Transfers_TrustFund       FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT FK_Transfers_FundDetails     FOREIGN KEY (fund_dt_id)
        REFERENCES FundDetails(fund_dt_id),
    CONSTRAINT FK_Transfers_FromShareholder FOREIGN KEY (from_sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT FK_Transfers_ToShareholder   FOREIGN KEY (to_sh_id)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT CHK_Transfers_type           CHECK (transfer_type IN ('sale', 'gift', 'inheritance')),
    CONSTRAINT CHK_Transfers_status         CHECK (status        IN ('pending', 'completed', 'cancelled')),
    CONSTRAINT CHK_Transfers_pct            CHECK (pct_transfer   > 0 AND pct_transfer <= 100),
    CONSTRAINT CHK_Transfers_price          CHECK (agreed_price  IS NULL OR agreed_price > 0),
    CONSTRAINT CHK_Transfers_different_sh   CHECK (from_sh_id   != to_sh_id),
    CONSTRAINT CHK_Transfers_dates          CHECK (transfer_date IS NULL OR transfer_date >= initiated_date)
);
GO


-- =============================================================================
-- 11. Expenses
-- =============================================================================
-- Operational costs attributed to a fund per month/year period.
-- A fund can have multiple expenses of different types in the same period.
-- paid_by references the shareholder or entity that fronted the cost.
-- approved_by references the admin who authorized the expense.
-- =============================================================================

CREATE TABLE Expenses (
    expense_id    INT            NOT NULL IDENTITY(1,1),
    fund_id       INT            NOT NULL,
    month         INT            NOT NULL,
    year          INT            NOT NULL,
    expense_type  VARCHAR(20)    NOT NULL,
    description   NVARCHAR(200)  NOT NULL,
    amount        DECIMAL(12,2)  NOT NULL,
    paid_on       DATE               NULL,
    paid_by       INT                NULL,
    status        VARCHAR(20)    NOT NULL DEFAULT 'pending',
    approved_by   INT                NULL,
    creation_date DATE           NOT NULL DEFAULT GETDATE(),
    notes         NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Expenses              PRIMARY KEY (expense_id),
    CONSTRAINT FK_Expenses_TrustFund    FOREIGN KEY (fund_id)
        REFERENCES TrustFund(fund_id),
    CONSTRAINT FK_Expenses_PaidBy       FOREIGN KEY (paid_by)
        REFERENCES Shareholder(sh_id),
    CONSTRAINT FK_Expenses_ApprovedBy   FOREIGN KEY (approved_by)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT CHK_Expenses_month       CHECK (month        BETWEEN 1 AND 12),
    CONSTRAINT CHK_Expenses_year        CHECK (year         BETWEEN 2000 AND 2100),
    CONSTRAINT CHK_Expenses_type        CHECK (expense_type IN ('maintenance', 'utility', 'tax', 'insurance', 'mgmt-fee', 'other')),
    CONSTRAINT CHK_Expenses_status      CHECK (status       IN ('pending', 'paid', 'disputed')),
    CONSTRAINT CHK_Expenses_amount      CHECK (amount        > 0)
);
GO


-- =============================================================================
-- 12. Logs
-- =============================================================================
-- Audit trail for every admin action across the system. One row per action.
-- table_name identifies which table was affected; record_id identifies which
-- row. old_info captures the previous value — the current value is always live
-- in the main table so new_info is intentionally excluded.
-- creation_date uses DATETIME2 for full timestamp precision.
-- =============================================================================

CREATE TABLE Logs (
    log_id         INT            NOT NULL IDENTITY(1,1),
    user_id        INT            NOT NULL,
    table_name     VARCHAR(60)    NOT NULL,
    record_id      INT            NOT NULL,
    action_details NVARCHAR(MAX)  NOT NULL,
    old_info       NVARCHAR(MAX)      NULL,
    creation_date  DATETIME2      NOT NULL DEFAULT GETDATE(),
    notes          NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Logs              PRIMARY KEY (log_id),
    CONSTRAINT FK_Logs_AdminUsers   FOREIGN KEY (user_id)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT CHK_Logs_table_name  CHECK (table_name IN (
        'Shareholder', 'Property',     'TrustFund',  'FundDetails',
        'Payments',    'AdminUsers',   'RentalIncome','Dividend',
        'SHBKAccounts','Transfers',    'Expenses',    'Configurations'
    ))
);
GO


-- =============================================================================
-- 13. Configurations
-- =============================================================================
-- System-wide dropdown value store. One row per configuration key; all allowed
-- values for that key are packed into the value column. The application reads
-- this table to populate UI dropdowns and filters on is_active = 1.
-- [key] is wrapped in brackets because KEY is a reserved word in SQL Server.
-- Rows should never be deleted — set is_active = 0 to retire an entry.
-- =============================================================================

CREATE TABLE Configurations (
    config_id   INT            NOT NULL IDENTITY(1,1),
    [key]       VARCHAR(100)   NOT NULL,
    value       NVARCHAR(MAX)  NOT NULL,
    is_active   BIT            NOT NULL DEFAULT 1,
    user_id     INT                NULL,
    last_edited DATETIME       NOT NULL DEFAULT GETDATE(),
    notes       NVARCHAR(MAX)      NULL,

    CONSTRAINT PK_Configurations        PRIMARY KEY (config_id),
    CONSTRAINT FK_Configurations_Admin  FOREIGN KEY (user_id)
        REFERENCES AdminUsers(user_id),
    CONSTRAINT UQ_Configurations_key    UNIQUE ([key])
);
GO


-- =============================================================================
-- 14. Deferred FK — FundDetails.transfer_id → Transfers
-- =============================================================================
-- FundDetails and Transfers have a circular dependency — each references the
-- other. This is resolved by creating both tables first without the mutual FK,
-- then adding it here once both tables exist.
-- transfer_id is NULL for original stakes; only populated when a FundDetails
-- row was created as a result of a completed Transfer.
-- =============================================================================

ALTER TABLE FundDetails
ADD CONSTRAINT FK_FundDetails_Transfers
    FOREIGN KEY (transfer_id)
    REFERENCES Transfers(transfer_id);
GO


-- =============================================================================
-- 15. Filtered Unique Index — Shareholder.CNIC
-- =============================================================================
-- SQL Server UNIQUE constraints treat NULL as equal to other NULLs, which
-- would incorrectly reject multiple shareholders with no CNIC (e.g. companies).
-- A filtered unique index scopes uniqueness only to rows where CNIC IS NOT NULL,
-- allowing multiple NULL values while still preventing duplicate CNIC entries.
-- =============================================================================

CREATE UNIQUE INDEX UX_Shareholder_CNIC
    ON Shareholder(CNIC)
    WHERE CNIC IS NOT NULL;
GO


-- =============================================================================
-- END OF SCHEMA
-- =============================================================================