-- ============================================================
-- BTK REIT Management Engine — Test Run Teardown Script
-- Version : 1.1.0
-- Author  : Rayder-23/BTK
--
-- PURPOSE:
--   Deletes ONLY the rows created by REIT_Test_Suite.http.
--   Identified by the "TEST-RUN" marker in name/description fields.
--   Preserves all production/seed data untouched.
--
-- GUARDRAILS:
--   ✔ Uses targeted WHERE clauses — no TRUNCATE, no CHECKIDENT.
--   ✔ Deletes in strict reverse-dependency order to avoid FK violations.
--   ✔ Wrapped in a transaction — rolls back entirely if any step fails.
--   ✔ Prints a row-count summary for every table touched.
--
-- HOW TO RUN:
--   sqlcmd -S TAZ\SQLEXPRESS -d REIT -i teardown_test_run.sql
--   — or open in SSMS and execute against the REIT database.
--
-- DELETION ORDER  (reverse of creation):
--   1. Logs         — audit entries authored against test record IDs
--   2. Dividend     — rows tied to test fund
--   3. Expenses     — rows tied to test fund
--   4. RentalIncome — rows tied to test fund
--   5. Payments     — rows tied to test fund
--   6. Transfers    — rows tied to test fund
--   7. FundDetails  — rows tied to test fund (active + retired)
--   8. TrustFund   — the test fund itself
--   9. SHBKAccounts — bank accounts for the test shareholder
--  10. Shareholders — the test shareholder row
--  11. Properties   — the test property row
-- ============================================================

USE REIT;
GO

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

PRINT '============================================================';
PRINT 'BTK REIT — Test Run Teardown Starting';
PRINT 'Database : ' + DB_NAME();
PRINT 'Time     : ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

    -- ──────────────────────────────────────────────────────────────────────────
    -- Resolve test entity IDs from marker strings.
    -- All test data is identified by the "TEST-RUN" prefix in name fields.
    -- ──────────────────────────────────────────────────────────────────────────

    -- Test shareholder: fullName LIKE 'TEST-RUN%'
    DECLARE @TestShId   INT = (
        SELECT sh_id FROM Shareholder
        WHERE fullName LIKE 'TEST-RUN%'
    );

    -- Test property: prop_name LIKE 'TEST-RUN%'
    DECLARE @TestPropId INT = (
        SELECT prop_id FROM Property
        WHERE prop_name LIKE 'TEST-RUN%'
    );

    -- Test fund: linked to the test property
    DECLARE @TestFundId INT = (
        SELECT fund_id FROM TrustFund
        WHERE prop_id = @TestPropId
    );

    PRINT 'Resolved test entity IDs:';
    PRINT '  @TestShId   = ' + ISNULL(CAST(@TestShId   AS VARCHAR), 'NULL — no test shareholder found');
    PRINT '  @TestPropId = ' + ISNULL(CAST(@TestPropId AS VARCHAR), 'NULL — no test property found');
    PRINT '  @TestFundId = ' + ISNULL(CAST(@TestFundId AS VARCHAR), 'NULL — no test fund found');
    PRINT '';

    -- ── Abort early if nothing to clean up ───────────────────────────────────
    IF @TestShId IS NULL AND @TestPropId IS NULL AND @TestFundId IS NULL
    BEGIN
        PRINT 'INFO: No TEST-RUN data found. Nothing to delete.';
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 1 — Logs
    -- Delete audit entries whose record_id matches any of the test entity IDs
    -- AND whose table_name matches the tables we created data in.
    -- This is the safest approach: scoped to both table_name and record_id.
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @LogsDeleted INT = 0;

    IF @TestShId IS NOT NULL
    BEGIN
        DELETE FROM Logs
        WHERE table_name IN ('Shareholder', 'SHBKAccounts')
          AND record_id IN (
              SELECT sh_id FROM Shareholder WHERE sh_id = @TestShId
              UNION ALL
              SELECT sh_account_id FROM SHBKAccounts WHERE sh_id = @TestShId
          );
        SET @LogsDeleted = @LogsDeleted + @@ROWCOUNT;
    END

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM Logs
        WHERE table_name IN (
                'TrustFund', 'FundDetails', 'Transfers',
                'Payments', 'RentalIncome', 'Expenses', 'Dividend'
              )
          AND record_id IN (
              SELECT fund_id        FROM TrustFund    WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT fund_dt_id     FROM FundDetails   WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT transfer_id    FROM Transfers     WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT payment_id     FROM Payments      WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT rent_id        FROM RentalIncome  WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT expense_id     FROM Expenses      WHERE fund_id   = @TestFundId
              UNION ALL
              SELECT div_id         FROM Dividend      WHERE fund_id   = @TestFundId
          );
        SET @LogsDeleted = @LogsDeleted + @@ROWCOUNT;
    END

    IF @TestPropId IS NOT NULL
    BEGIN
        DELETE FROM Logs
        WHERE table_name = 'Property'
          AND record_id  = @TestPropId;
        SET @LogsDeleted = @LogsDeleted + @@ROWCOUNT;
    END

    PRINT 'STEP 1 — Logs deleted           : ' + CAST(@LogsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 2 — Dividend
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @DividendsDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM Dividend WHERE fund_id = @TestFundId;
        SET @DividendsDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 2 — Dividends deleted      : ' + CAST(@DividendsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 3 — Expenses
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @ExpensesDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM Expenses WHERE fund_id = @TestFundId;
        SET @ExpensesDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 3 — Expenses deleted       : ' + CAST(@ExpensesDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 4 — RentalIncome
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @RentalDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM RentalIncome WHERE fund_id = @TestFundId;
        SET @RentalDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 4 — RentalIncome deleted   : ' + CAST(@RentalDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 5 — Payments
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @PaymentsDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM Payments WHERE fund_id = @TestFundId;
        SET @PaymentsDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 5 — Payments deleted       : ' + CAST(@PaymentsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 6 — Transfers
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @TransfersDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM Transfers WHERE fund_id = @TestFundId;
        SET @TransfersDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 6 — Transfers deleted      : ' + CAST(@TransfersDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 7 — FundDetails  (active rows and retired/closed rows)
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @FundDetailsDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM FundDetails WHERE fund_id = @TestFundId;
        SET @FundDetailsDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 7 — FundDetails deleted    : ' + CAST(@FundDetailsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 8 — TrustFund
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @FundsDeleted INT = 0;

    IF @TestFundId IS NOT NULL
    BEGIN
        DELETE FROM TrustFund WHERE fund_id = @TestFundId;
        SET @FundsDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 8 — TrustFund deleted     : ' + CAST(@FundsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 9 — SHBKAccounts  (bank accounts for the test shareholder)
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @AccountsDeleted INT = 0;

    IF @TestShId IS NOT NULL
    BEGIN
        DELETE FROM SHBKAccounts WHERE sh_id = @TestShId;
        SET @AccountsDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 9 — SHBKAccounts deleted   : ' + CAST(@AccountsDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 10 — Shareholders
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @ShareholdersDeleted INT = 0;

    IF @TestShId IS NOT NULL
    BEGIN
        DELETE FROM Shareholder WHERE sh_id = @TestShId;
        SET @ShareholdersDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 10 — Shareholders deleted  : ' + CAST(@ShareholdersDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- STEP 11 — Properties
    -- ──────────────────────────────────────────────────────────────────────────
    DECLARE @PropertiesDeleted INT = 0;

    IF @TestPropId IS NOT NULL
    BEGIN
        DELETE FROM Property WHERE prop_id = @TestPropId;
        SET @PropertiesDeleted = @@ROWCOUNT;
    END

    PRINT 'STEP 11 — Properties deleted    : ' + CAST(@PropertiesDeleted AS VARCHAR);

    -- ──────────────────────────────────────────────────────────────────────────
    -- Summary
    -- ──────────────────────────────────────────────────────────────────────────
    PRINT '';
    PRINT '============================================================';
    PRINT 'Teardown Summary';
    PRINT '============================================================';

    DECLARE @TotalDeleted INT =
        @LogsDeleted + @DividendsDeleted + @ExpensesDeleted +
        @RentalDeleted + @PaymentsDeleted + @TransfersDeleted +
        @FundDetailsDeleted + @FundsDeleted + @AccountsDeleted +
        @ShareholdersDeleted + @PropertiesDeleted;

    PRINT 'Total rows deleted : ' + CAST(@TotalDeleted AS VARCHAR);
    PRINT '';

    -- Verify preserved data still intact
    PRINT 'Preserved table counts (spot-check):';
    SELECT 'AdminUsers'     AS [Table], COUNT(*) AS [Rows] FROM AdminUsers
    UNION ALL
    SELECT 'Configurations',            COUNT(*)            FROM Configurations;

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '>>> Teardown complete. All TEST-RUN data removed.';
    PRINT '>>> Production/seed data untouched.';

END TRY
BEGIN CATCH

    ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '!!! TEARDOWN FAILED — transaction rolled back !!!';
    PRINT 'Error  : ' + ERROR_MESSAGE();
    PRINT 'Line   : ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'State  : ' + CAST(ERROR_STATE() AS VARCHAR);

    -- Re-raise so the calling process knows it failed.
    THROW;

END CATCH
GO
