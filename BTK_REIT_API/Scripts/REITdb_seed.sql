-- =============================================================================
-- REIT Database Seed Data
-- =============================================================================
-- Description : Development seed data for the REIT database. Covers all 13
--               tables with a realistic scenario: one property, one fund,
--               three shareholders (including the REIT itself), a completed
--               transfer, one month of rental income, dividend payouts,
--               a payment record, an expense, and system configuration.
--
-- Prerequisites: Schema must be applied first via REIT_Schema.sql
-- Database     : SQL Server Express
-- Version      : 1.0
-- Created      : 2026-04-29
-- =============================================================================
-- SEEDING ORDER
--   1.  Shareholder
--   2.  AdminUsers
--   3.  Property
--   4.  TrustFund
--   5.  FundDetails       — initial stakes (REIT 10%, Ahmed 55%, Sara 35%)
--   6.  SHBKAccounts
--   7.  RentalIncome
--   8.  Transfers         — Sara sells 15% to Ahmed
--   9.  FundDetails       — updated stakes post-transfer
--   10. Payments
--   11. Dividend
--   12. Expenses
--   13. Logs
--   14. Configurations
-- =============================================================================


USE REIT;
GO


-- =============================================================================
-- 1. Shareholder
-- =============================================================================
-- Three shareholders:
--   sh_id = 1  REIT Fund itself      (is_reit = 1, holds mandatory 10%)
--   sh_id = 2  Ahmed Khan            (individual investor)
--   sh_id = 3  Sara Mahmood          (individual investor)
-- =============================================================================

INSERT INTO Shareholder (sh_type, userName, password, fullName, CNIC, NTN_no, passport_no, contactNo, contactEmail, is_filler, is_overseas, is_reit, creationDate, status)
VALUES
    ('company',    'REIT_FUND', NULL, 'Karachi Property REIT', NULL,               'NTN-0001', NULL, '02135001000', 'fund@kpreit.com',   0, 0, 1, '2024-01-01', 'active'),
    ('individual', 'ahmed_k',   NULL, 'Ahmed Khan',            '42101-1234567-1',  NULL,       NULL, '03001234567', 'ahmed@email.com',   0, 0, 0, '2024-01-10', 'active'),
    ('individual', 'sara_m',    NULL, 'Sara Mahmood',          '42201-7654321-2',  NULL,       NULL, '03217654321', 'sara@email.com',    0, 0, 0, '2024-01-10', 'active');
GO


-- =============================================================================
-- 2. AdminUsers
-- =============================================================================
-- Two admins:
--   user_id = 1  admin_root   root administrator, no created_by
--   user_id = 2  admin_2      created by admin_root
-- =============================================================================

INSERT INTO AdminUsers (userName, security_level, password, created_by, status, creation_date)
VALUES
    ('admin_root', 1, 'hashed_password_here', NULL, 'active', '2024-01-01'),
    ('admin_2',    2, 'hashed_password_here', 1,    'active', '2024-01-05');
GO


-- =============================================================================
-- 3. Property
-- =============================================================================
-- One property: Clifton Tower, Karachi
-- =============================================================================

INSERT INTO Property (prop_type, prop_name, address, city, province_state, country, date_added, purchase_price, current_value, status)
VALUES
    ('commercial', 'Clifton Tower', '24-B Khayaban-e-Ittehad', 'Karachi', 'Sindh', 'Pakistan', '2024-01-15', 15000000.00, 16500000.00, 'active');
GO


-- =============================================================================
-- 4. TrustFund
-- =============================================================================
-- One fund for Clifton Tower.
-- fund_total_value matches purchase_price as the starting basis.
-- =============================================================================

INSERT INTO TrustFund (prop_id, fund_title, fund_total_value, creationDate)
VALUES
    (1, 'Clifton Tower Fund', 15000000.00, '2024-01-15');
GO


-- =============================================================================
-- 5. FundDetails — Initial Stakes
-- =============================================================================
-- Original ownership split at fund inception:
--   fund_dt_id = 1  REIT     10%  share_value =  1,500,000
--   fund_dt_id = 2  Ahmed    55%  share_value =  8,250,000
--   fund_dt_id = 3  Sara     35%  share_value =  5,250,000
-- All three rows active (end_date = NULL), no transfer_id (original stakes).
-- Rows 2 and 3 will be closed after the transfer in Section 9.
-- =============================================================================

INSERT INTO FundDetails (fund_id, sh_id, pct_owned, share_value, acquired_date, end_date, transfer_id)
VALUES
    (1, 1, 10.00, 1500000.00, '2024-01-15', NULL, NULL),   -- REIT 10%   (fund_dt_id = 1)
    (1, 2, 55.00, 8250000.00, '2024-01-15', NULL, NULL),   -- Ahmed 55%  (fund_dt_id = 2)
    (1, 3, 35.00, 5250000.00, '2024-01-15', NULL, NULL);   -- Sara 35%   (fund_dt_id = 3)
GO


-- =============================================================================
-- 6. SHBKAccounts
-- =============================================================================
-- One verified bank account per shareholder.
-- The REIT fund does not receive dividends so has no bank account.
--   sh_account_id = 1  Ahmed  HBL
--   sh_account_id = 2  Sara   Meezan
-- =============================================================================

INSERT INTO SHBKAccounts (sh_id, bank, account_title, acNo, status, approved_by, creation_date)
VALUES
    (2, 'HBL',    'Ahmed Khan',   'PK36HABB0000000123456702', 'active', 1, '2024-01-12'),
    (3, 'Meezan', 'Sara Mahmood', 'PK24MEZN0000000987654321', 'active', 1, '2024-01-12');
GO


-- =============================================================================
-- 7. RentalIncome
-- =============================================================================
-- April 2026 rent for Clifton Tower.
-- Rent was due on the 5th and paid on the 4th — one day early, no late fee.
-- =============================================================================

INSERT INTO RentalIncome (fund_id, rent_month, rent_year, due_date, amount_due, amount_paid, late_fee, payment_date, status)
VALUES
    (1, 4, 2026, '2026-04-05', 150000.00, 150000.00, 0.00, '2026-04-04', 'paid');
GO


-- =============================================================================
-- 8. Transfers
-- =============================================================================
-- Sara sells 15% of her stake to Ahmed.
-- Before transfer:  Ahmed 55%,  Sara 35%
-- After transfer:   Ahmed 70%,  Sara 20%
--
-- transfer_id = 1 references fund_dt_id = 3 (Sara's original stake).
-- On completion, FundDetails rows 2 and 3 are closed and new rows opened.
-- =============================================================================

INSERT INTO Transfers (transfer_type, approved_by, fund_id, fund_dt_id, from_sh_id, to_sh_id, pct_transfer, agreed_price, initiated_date, transfer_date, status)
VALUES
    ('sale', 1, 1, 3, 3, 2, 15.00, 2250000.00, '2026-04-01', '2026-04-10', 'completed');
GO


-- =============================================================================
-- 9. FundDetails — Post-Transfer Stakes
-- =============================================================================
-- Close Sara's original stake (fund_dt_id = 3) and Ahmed's original stake
-- (fund_dt_id = 2), then open new rows reflecting updated percentages.
--
-- Step 1: Close old stakes
-- =============================================================================

UPDATE FundDetails SET end_date = '2026-04-10' WHERE fund_dt_id = 3;   -- Sara old (35%)
UPDATE FundDetails SET end_date = '2026-04-10' WHERE fund_dt_id = 2;   -- Ahmed old (55%)
GO

-- Step 2: Open new stakes (transfer_id = 1 links back to the completed transfer)

INSERT INTO FundDetails (fund_id, sh_id, pct_owned, share_value, acquired_date, end_date, transfer_id)
VALUES
    (1, 2, 70.00, 10500000.00, '2026-04-10', NULL, 1),    -- Ahmed 70%  (fund_dt_id = 4)
    (1, 3, 20.00,  3000000.00, '2026-04-10', NULL, 1);    -- Sara 20%   (fund_dt_id = 5)
GO


-- =============================================================================
-- 10. Payments
-- =============================================================================
-- Ahmed pays for his acquired 15% stake.
-- gross = agreed_price (2,250,000), tax = 10% (225,000), net = 2,025,000.
-- fund_dt_id = 4 is Ahmed's new FundDetails row created post-transfer.
-- =============================================================================

INSERT INTO Payments (sh_id, fund_id, fund_dt_id, gross_fund_amount, tax, additional_payments, net_amount_due, amount_paid, payment_date, payment_type, bank, ds_no, status, approved_by, creation_date)
VALUES
    (2, 1, 4, 2250000.00, 225000.00, 0.00, 2025000.00, 2025000.00, '2026-04-10', 'bank-transfer', 'HBL', 'DS-2026-0042', 'paid', 1, '2026-04-10');
GO


-- =============================================================================
-- 11. Dividend
-- =============================================================================
-- April 2026 dividends distributed based on post-transfer ownership:
--   Ahmed 70%: gross = 105,000  tax = 10,500  net = 94,500
--   Sara  20%: gross =  30,000  tax =  3,000  net = 27,000
--   REIT  10%: retains 15,000 — no dividend row created for the REIT
-- =============================================================================

INSERT INTO Dividend (div_type, sh_id, fund_id, fund_dt_id, account_id, month, year, gross_div_amount, tax, deduction, net_amount_paid, paid_on, payment_method, status)
VALUES
    ('regular', 2, 1, 4, 1, 4, 2026, 105000.00, 10500.00, 0.00, 94500.00, '2026-04-15', 'bank-transfer', 'paid'),
    ('regular', 3, 1, 5, 2, 4, 2026,  30000.00,  3000.00, 0.00, 27000.00, '2026-04-15', 'bank-transfer', 'paid');
GO


-- =============================================================================
-- 12. Expenses
-- =============================================================================
-- One maintenance expense for April 2026.
-- paid_by = 2 (Ahmed fronted the cost), approved_by = 1 (admin_root).
-- =============================================================================

INSERT INTO Expenses (fund_id, month, year, expense_type, description, amount, paid_on, paid_by, status, approved_by, creation_date)
VALUES
    (1, 4, 2026, 'maintenance', 'Lobby renovation and repainting', 25000.00, '2026-04-20', 2, 'paid', 1, '2026-04-20');
GO


-- =============================================================================
-- 13. Logs
-- =============================================================================
-- Audit trail covering all key actions performed during the seed scenario.
-- =============================================================================

INSERT INTO Logs (user_id, table_name, record_id, action_details, old_info)
VALUES
    (1, 'Transfers',   1, 'Approved transfer of 15% stake from Sara Mahmood to Ahmed Khan',          NULL),
    (1, 'FundDetails', 3, 'Closed Sara Mahmood original stake after transfer completion',            'pct_owned: 35.00, end_date: NULL'),
    (1, 'FundDetails', 2, 'Closed Ahmed Khan original stake after transfer completion',              'pct_owned: 55.00, end_date: NULL'),
    (1, 'FundDetails', 4, 'Opened new stake for Ahmed Khan post-transfer — 70%',                    NULL),
    (1, 'FundDetails', 5, 'Opened new stake for Sara Mahmood post-transfer — 20%',                  NULL),
    (1, 'Payments',    1, 'Created payment record for Ahmed Khan — Transfer 1, amount 2,025,000',   NULL),
    (1, 'Dividend',    1, 'Created regular dividend for Ahmed Khan — April 2026, net 94,500',        NULL),
    (1, 'Dividend',    2, 'Created regular dividend for Sara Mahmood — April 2026, net 27,000',      NULL),
    (1, 'Expenses',    1, 'Recorded maintenance expense — Lobby renovation, 25,000, fund_id 1',     NULL);
GO


-- =============================================================================
-- 14. Configurations
-- =============================================================================
-- Dropdown value store. One row per key, all values comma-separated.
-- The application filters on is_active = 1 to populate UI dropdowns.
-- =============================================================================

INSERT INTO Configurations ([key], value, is_active, user_id)
VALUES
    ('sh_type',        'individual,company,trust',                         1, 1),
    ('prop_type',      'residential,commercial,mixed-use',                  1, 1),
    ('expense_type',   'maintenance,utility,tax,insurance,mgmt-fee,other',  1, 1),
    ('payment_type',   'bank-transfer,cheque,cash',                         1, 1),
    ('transfer_type',  'sale,gift,inheritance',                             1, 1),
    ('div_type',       'regular,special',                                   1, 1),
    ('banks',          'HBL,Meezan,UBL,MCB,Allied,Standard Chartered',      1, 1),
    ('status_sh',      'active,suspended,exited',                           1, 1),
    ('status_prop',    'active,sold,under-review',                          1, 1),
    ('status_pay',     'pending,paid,partial,overdue',                      1, 1),
    ('status_div',     'pending,paid,on-hold',                              1, 1),
    ('status_exp',     'pending,paid,disputed',                             1, 1),
    ('security_levels','1,2,3',                                             1, 1);
GO


-- =============================================================================
-- VERIFICATION
-- =============================================================================
-- Run the queries below to confirm all tables seeded correctly.
-- =============================================================================

-- Row counts across all tables
SELECT t.name AS table_name, p.rows AS row_count
FROM sys.tables t
JOIN sys.partitions p ON t.object_id = p.object_id
WHERE p.index_id IN (0, 1)
ORDER BY t.name;

-- Current active ownership (post-transfer)
SELECT
    s.fullName,
    fd.pct_owned,
    fd.share_value,
    fd.acquired_date,
    CASE WHEN fd.end_date IS NULL THEN 'active' ELSE 'closed' END AS stake_status
FROM FundDetails fd
JOIN Shareholder s ON fd.sh_id = s.sh_id
ORDER BY fd.fund_dt_id;

-- April 2026 dividend summary
SELECT
    s.fullName,
    d.gross_div_amount,
    d.tax,
    d.net_amount_paid,
    a.bank,
    d.status
FROM Dividend d
JOIN Shareholder  s ON d.sh_id      = s.sh_id
JOIN SHBKAccounts a ON d.account_id = a.sh_account_id;
GO

-- =============================================================================
-- END OF SEED DATA
-- =============================================================================