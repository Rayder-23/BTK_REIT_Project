# Test Run Report — 2026-04-11

**Suite**: `Scripts/REIT_Test_Suite.http`  
**Teardown**: `Scripts/teardown_test_run.sql`  
**API**: `dotnet run` — HTTP `http://localhost:5039`  
**Database**: `TAZ\SQLEXPRESS` → `REIT`

---

## Lifecycle Steps — 8/8 PASS

| Step | Endpoint | Status | Key Output |
|------|----------|--------|------------|
| 1 | `POST /api/shareholders` | 201 ✔ | `shId=10` |
| 2 | `POST /api/shareholders/account` | 201 ✔ | `shAccountId=6` |
| 3 | `POST /api/properties/onboard` | 201 ✔ | `propId=5`, `fundId=5`, `fundDtId=12` |
| 4a | `POST /api/transfers/initiate` | 200 ✔ | `transferId=7` |
| 4b | `POST /api/transfers/complete/7?userId=1` | 200 ✔ | `verifiedTotalPct=100.00` |
| 5a | `POST /api/rental/record` | 201 ✔ | `rentId=3`, `status=overdue` |
| 5b | `PATCH /api/rental/receive-payment/3` | 200 ✔ | `status=paid` |
| 6a | `POST /api/dividend/calculate` | 200 ✔ | `dividendsCreated=1`, `dividendIds=[4]` |
| 6b | `POST /api/dividend/confirm-payout/4` | 200 ✔ | `status=paid`, `paymentMethod=bank-transfer` |
| 7a | `POST /api/expense/record` | 201 ✔ | `expenseId=5`, `status=pending` |
| 7b | `PATCH /api/expense/settle/5` | 200 ✔ | `status=paid` |
| 8 | `GET /api/log/recent?limit=20` | 200 ✔ | 12 entries for test run |
| 8c | `GET /api/Fund/5/summary` | 200 ✔ | 2 owners, rental paid, expenses paid |

**Dividend maths (Ahmed Raza, 10% stake, 10% tax rate):**

| | Amount |
|-|--------|
| Gross | 25,000.00 |
| Tax | 2,500.00 |
| Net Paid | 22,500.00 |

---

## Failure / Validation Tests — 5/5 PASS

| Test | Endpoint | Expected | Result |
|------|----------|----------|--------|
| 1 — Invalid shareholder status `"abandoned"` | `PATCH /api/shareholders/10/status` | 400 | 400 ✔ |
| 2 — Invalid expense type `"bribery"` | `POST /api/expense/record` | 400 | 400 ✔ |
| 3 — Duplicate dividend calculation | `POST /api/dividend/calculate` | 409 | 409 ✔ |
| 4 — Double-settle expense | `PATCH /api/expense/settle/5` | 400 | 400 ✔ |
| 5 — Double-complete transfer | `POST /api/transfers/complete/7` | 400 | 400 ✔ |

---

## Teardown

Script executed successfully. **42 rows deleted** across all transactional tables.

| Table | Rows Deleted |
|-------|-------------|
| Logs | 31 |
| Dividend | 1 |
| Expenses | 1 |
| RentalIncome | 1 |
| Payments | 0 (gift transfer — no payment row created) |
| Transfers | 1 |
| FundDetails | 3 (seed row + seller residual + buyer row) |
| TrustFund | 1 |
| SHBKAccounts | 1 |
| Shareholder | 1 |
| Property | 1 |
| **Total** | **42** |

Preserved: `AdminUsers=2`, `Configurations=17` — production seed data untouched.

---

## Bugs Found & Fixed During Run

### 1 — Wrong `expenseType` value in test suite
- **Problem**: Suite sent `"management"` but the `expense_type` config key holds `maintenance, utility, tax, insurance, mgmt-fee, other`. ValidationService rejected it with 400.
- **Fix**: Changed to `"mgmt-fee"` in `REIT_Test_Suite.http`.

### 2 — `CHK_FundDetails_dates` constraint violation on same-day transfer
- **Problem**: `CompleteTransfer` (Step 4b) failed with a SQL CHECK constraint error. The constraint enforced `end_date > acquired_date` (strict inequality). When the fund was onboarded and the transfer completed on the same calendar day, setting `end_date = acquired_date` violated it.
- **Temporary fix (Run 1)**: Added `"dateAdded": "2026-04-10"` to the onboard request so `acquired_date` was yesterday and `end_date` (today) satisfied the constraint.
- **Permanent fix (Run 2)**: Schema patched via `Scripts/alter_chk_funddetails_dates.sql` — constraint relaxed to `>=`. Backdating workaround removed from `REIT_Test_Suite.http`. See schema patch section below.

### 3 — Wrong table names in teardown script
- **Problem**: `teardown_test_run.sql` used plural EF names (`TrustFunds`, `Shareholders`, `Properties`) instead of the actual DB table names (`TrustFund`, `Shareholder`, `Property`).
- **Fix**: Corrected all table references in the teardown script.

### 4 — Wrong column name in teardown script
- **Problem**: Used `full_name` (snake_case) when the actual DB column is `fullName` (camelCase).
- **Fix**: Corrected to `fullName` in the `WHERE` clause used to resolve `@TestShId`.

### 5 — `SET QUOTED_IDENTIFIER` error in teardown script
- **Problem**: SQL Server rejected `DELETE` statements due to filtered indexes requiring `SET QUOTED_IDENTIFIER ON`.
- **Fix**: Added `SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON;` at the top of the script.

### 6 — Em-dash encoding error in `propNotes` / `fundNotes`
- **Problem**: The `—` (U+2014 em-dash) in the notes strings caused a JSON parse error (`$.propNotes could not be converted to System.String`) when sent via `curl` in a bash shell due to encoding issues.
- **Fix**: Replaced em-dashes with plain ASCII hyphens (`-`) in the test suite strings.

---

## Schema Patch — `CHK_FundDetails_dates` (Run 2, 2026-04-11)

**Script**: `Scripts/alter_chk_funddetails_dates.sql`

| | Value |
|-|-------|
| Old constraint | `[end_date] IS NULL OR [end_date] > [acquired_date]` |
| New constraint | `[end_date] IS NULL OR [end_date] >= [acquired_date]` |
| Rationale | Strict `>` blocked same-day transfers — a valid operational scenario where a fund is onboarded and a stake transferred within the same calendar day |

### Validation Test Results

| Case | `acquired_date` | `end_date` | Result |
|------|----------------|-----------|--------|
| Same-day transfer (new) | 2026-04-11 | 2026-04-11 | PASS ✔ — accepted |
| Earlier end_date (guard) | 2026-04-11 | 2026-04-10 | PASS ✔ — rejected by constraint |

### Run 2 — Same-Day Transfer Test (Post-Patch)

Full lifecycle re-run with no `dateAdded` in the onboard request (`acquired_date` defaulted to today):

| Step | Endpoint | Result | Key Output |
|------|----------|--------|------------|
| 1 | `POST /api/shareholders` | 201 ✔ | `shId=11` |
| 2 | `POST /api/shareholders/account` | 201 ✔ | `shAccountId=7` |
| 3 | `POST /api/properties/onboard` | 201 ✔ | `propId=6`, `fundId=6`, `fundDtId=15` — `acquired_date=2026-04-11` |
| 4a | `POST /api/transfers/initiate` | 200 ✔ | `transferId=8` |
| 4b | `POST /api/transfers/complete/8?userId=1` | 200 ✔ | `verifiedTotalPct=100.00` — `end_date=acquired_date=2026-04-11` |

Teardown: **20 rows deleted**. Production seed data untouched.
