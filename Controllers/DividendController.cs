using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DividendController : ControllerBase
    {
        private const int ReitShareholderId = 1;

        private readonly ReitContext _context;
        private readonly IAuditService _audit;

        public DividendController(ReitContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── POST /api/Dividend/calculate ─────────────────────────────────────
        /// <summary>
        /// Prepares one 'pending' Dividend row per eligible shareholder for the
        /// specified fund and rental period.
        ///
        /// Prerequisites (checked before opening the transaction):
        ///   1. RentalIncome for the period exists with status 'paid' or 'partial'.
        ///   2. Active FundDetails for the fund sum to exactly 100.00%.
        ///   3. Every non-REIT active stakeholder has at least one active SHBKAccount.
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculateDividends([FromBody] DividendCalculateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── Pre-flight checks (outside the transaction — fail fast) ────────

            // 1. Verify RentalIncome exists and is in a distributable state.
            var income = await _context.RentalIncomes
                .FirstOrDefaultAsync(ri =>
                    ri.FundId    == dto.FundId &&
                    ri.RentMonth == dto.RentMonth &&
                    ri.RentYear  == dto.RentYear);

            if (income == null)
                return NotFound(
                    $"No RentalIncome record found for FundID {dto.FundId}, " +
                    $"period {dto.RentMonth}/{dto.RentYear}.");

            if (!income.Status.Equals("paid",    StringComparison.OrdinalIgnoreCase) &&
                !income.Status.Equals("partial", StringComparison.OrdinalIgnoreCase))
                return BadRequest(
                    $"RentalIncome rent_id={income.RentId} has status '{income.Status}'. " +
                    "Dividends can only be calculated when status is 'paid' or 'partial'.");

            // 2. Check no dividends have already been calculated for this period.
            bool alreadyCalculated = await _context.Dividends.AnyAsync(d =>
                d.FundId == dto.FundId &&
                d.Month  == dto.RentMonth &&
                d.Year   == dto.RentYear);

            if (alreadyCalculated)
                return Conflict(
                    $"Dividend rows already exist for FundID {dto.FundId}, " +
                    $"period {dto.RentMonth}/{dto.RentYear}. Use confirm-payout to process them.");

            // 3. Load all active (non-REIT) stakeholders with their best active bank account.
            var activeStakes = await _context.FundDetails
                .Where(fd => fd.FundId == dto.FundId &&
                             fd.EndDate == null &&
                             fd.ShId != ReitShareholderId)
                .Select(fd => new
                {
                    fd.FundDtId,
                    fd.ShId,
                    fd.PctOwned,
                    ActiveAccount = fd.Sh.Shbkaccounts
                        .Where(a => a.Status == "active")
                        .Select(a => (int?)a.ShAccountId)
                        .FirstOrDefault()
                })
                .ToListAsync();

            if (activeStakes.Count == 0)
                return BadRequest(
                    $"No eligible (non-REIT) active FundDetails found for FundID {dto.FundId}.");

            // 4. Ownership integrity check — active stakes must sum to exactly 100.00%.
            //    (Include the REIT's slice in the sum; it is simply not paid out.)
            decimal totalPct = await _context.FundDetails
                .Where(fd => fd.FundId == dto.FundId && fd.EndDate == null)
                .SumAsync(fd => fd.PctOwned);

            if (totalPct != 100.00m)
                return UnprocessableEntity(
                    $"Ownership integrity check failed: active stakes in FundID {dto.FundId} " +
                    $"sum to {totalPct:F2}%, not 100.00%. Resolve ownership before distributing.");

            // 5. Ensure every eligible stakeholder has an active bank account.
            var missingAccounts = activeStakes
                .Where(s => s.ActiveAccount == null)
                .Select(s => s.ShId)
                .ToList();

            if (missingAccounts.Count > 0)
                return BadRequest(new
                {
                    Message = "The following shareholders have no active bank account in SHBKAccounts. " +
                              "Add or activate their accounts before calculating dividends.",
                    ShareholderIds = missingAccounts
                });

            // ── All checks passed — open the transaction ───────────────────────
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    int dividendsCreated = 0;
                    var createdDivIds = new List<int>();

                    foreach (var stake in activeStakes)
                    {
                        // All currency arithmetic stays in decimal throughout.
                        decimal gross = (stake.PctOwned / 100m) * income.AmountPaid;
                        decimal tax   = Math.Round(gross * dto.TaxRate, 2, MidpointRounding.AwayFromZero);
                        decimal net   = gross - tax;

                        var dividend = new Dividend
                        {
                            DivType        = "regular",
                            ShId           = stake.ShId,
                            FundId         = dto.FundId,
                            FundDtId       = stake.FundDtId,
                            AccountId      = stake.ActiveAccount!.Value,
                            Month          = dto.RentMonth,
                            Year           = dto.RentYear,
                            GrossDivAmount = gross,
                            Tax            = tax,
                            Deduction      = 0m,
                            NetAmountPaid  = net,
                            PaidOn         = null,           // Stamped on confirm-payout
                            PaymentMethod  = null,           // Stamped on confirm-payout
                            Status         = "pending",
                            Notes          = dto.Notes
                                             ?? $"Dividend for RentID {income.RentId}, " +
                                                $"period {dto.RentMonth}/{dto.RentYear}."
                        };

                        _context.Dividends.Add(dividend);
                        await _context.SaveChangesAsync();   // Populates dividend.DivId

                        _audit.LogAction(
                            userId: dto.UserId,
                            tableName: "Dividend",
                            recordId: dividend.DivId,
                            action: $"INSERT: Dividend calculated for ShareholderID {stake.ShId}, " +
                                    $"FundID {dto.FundId}, period {dto.RentMonth}/{dto.RentYear}. " +
                                    $"Gross={gross:F2}, Tax={tax:F2}, Net={net:F2}, Status=pending.");

                        createdDivIds.Add(dividend.DivId);
                        dividendsCreated++;
                    }

                    // Persist all staged audit logs in one final save.
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        Message          = $"{dividendsCreated} dividend row(s) created with status 'pending'.",
                        FundId           = dto.FundId,
                        Period           = $"{dto.RentMonth}/{dto.RentYear}",
                        RentId           = income.RentId,
                        DividendsCreated = dividendsCreated,
                        TaxRate          = dto.TaxRate,
                        DividendIds      = createdDivIds
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Dividend calculation failed: {ex.Message}");
                }
            });
        }

        // ── POST /api/Dividend/confirm-payout/{id} ───────────────────────────
        /// <summary>
        /// Stamps the actual bank transfer on a single pending Dividend row.
        /// Sets status = 'paid', paid_on = today (or provided date), payment_method = 'bank-transfer'.
        /// </summary>
        [HttpPost("confirm-payout/{id:int}")]
        public async Task<IActionResult> ConfirmPayout(int id, [FromBody] DividendPayoutDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var dividend = await _context.Dividends
                        .FirstOrDefaultAsync(d => d.DivId == id);

                    if (dividend == null)
                        return NotFound($"Dividend with div_id={id} not found.");

                    if (dividend.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"Dividend div_id={id} is already marked 'paid' " +
                            $"(paid_on={dividend.PaidOn}).");

                    if (!dividend.Status.Equals("pending", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"Dividend div_id={id} has status '{dividend.Status}' and cannot be confirmed.");

                    string  oldStatus = dividend.Status;
                    DateOnly paidOn   = dto.PaidOn ?? DateOnly.FromDateTime(DateTime.Today);

                    dividend.Status        = "paid";
                    dividend.PaidOn        = paidOn;
                    dividend.PaymentMethod = "bank-transfer";

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                        dividend.Notes = dto.Notes;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Dividend",
                        recordId: dividend.DivId,
                        action: $"UPDATE: Dividend div_id={id} confirmed as paid. " +
                                $"PaidOn={paidOn}, PaymentMethod=bank-transfer.",
                        oldInfo: $"Status={oldStatus}, PaidOn=null");

                    await _context.SaveChangesAsync();   // Persists dividend update + audit log
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        dividend.DivId,
                        dividend.ShId,
                        dividend.FundId,
                        dividend.Month,
                        dividend.Year,
                        dividend.GrossDivAmount,
                        dividend.Tax,
                        dividend.NetAmountPaid,
                        dividend.Status,
                        dividend.PaidOn,
                        dividend.PaymentMethod
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Dividend payout confirmation failed: {ex.Message}");
                }
            });
        }
    }
}
