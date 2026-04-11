using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/trustfunds")]
    [Tags("Trust Funds")]
    public class TrustFundsController : ControllerBase
    {
        private readonly ReitContext _context;

        public TrustFundsController(ReitContext context)
        {
            _context = context;
        }

        // ── GET /api/Fund/{id}/summary ───────────────────────────────────────
        /// <summary>
        /// Returns a consolidated health view of the fund: title, total value,
        /// active owners, most recent rental record, and expense totals for
        /// the current year.
        /// Route uses the singular 'Fund' prefix per the API spec.
        /// </summary>
        [HttpGet("/api/Fund/{id:int}/summary")]
        public async Task<IActionResult> GetFundSummary(int id)
        {
            var fund = await _context.TrustFunds
                .AsNoTracking()
                .Where(f => f.FundId == id)
                .Select(f => new { f.FundId, f.FundTitle, f.FundTitle1, f.FundTotalValue })
                .FirstOrDefaultAsync();

            if (fund == null)
                return NotFound($"TrustFund with fund_id={id} not found.");

            // Active owners — FundDetails with no end_date.
            var owners = await _context.FundDetails
                .AsNoTracking()
                .Where(fd => fd.FundId == id && fd.EndDate == null)
                .Select(fd => new FundOwnerDto
                {
                    ShId         = fd.ShId,
                    FullName     = fd.Sh.FullName,
                    PctOwned     = fd.PctOwned,
                    ShareValue   = fd.ShareValue,
                    AcquiredDate = fd.AcquiredDate
                })
                .ToListAsync();

            // Most recent rental income record.
            var latestRental = await _context.RentalIncomes
                .AsNoTracking()
                .Where(r => r.FundId == id)
                .OrderByDescending(r => r.RentYear)
                .ThenByDescending(r => r.RentMonth)
                .Select(r => new FundRentalSummaryDto
                {
                    RentId     = r.RentId,
                    RentMonth  = r.RentMonth,
                    RentYear   = r.RentYear,
                    AmountDue  = r.AmountDue,
                    AmountPaid = r.AmountPaid,
                    Status     = r.Status
                })
                .FirstOrDefaultAsync();

            // Expense totals for the current calendar year.
            int currentYear = DateTime.UtcNow.Year;

            var expenseTotals = await _context.Expenses
                .AsNoTracking()
                .Where(e => e.FundId == id && e.Year == currentYear)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalPaid    = g.Where(e => e.Status == "paid").Sum(e => e.Amount),
                    TotalPending = g.Where(e => e.Status == "pending").Sum(e => e.Amount)
                })
                .FirstOrDefaultAsync();

            var summary = new FundSummaryDto
            {
                FundId         = fund.FundId,
                FundTitle      = fund.FundTitle ?? fund.FundTitle1 ?? "",
                FundTotalValue = fund.FundTotalValue,
                ActiveOwners   = owners,
                LatestRental   = latestRental,
                Expenses       = new FundExpenseSummaryDto
                {
                    Year         = currentYear,
                    TotalPaid    = expenseTotals?.TotalPaid    ?? 0m,
                    TotalPending = expenseTotals?.TotalPending ?? 0m
                }
            };

            return Ok(summary);
        }


        // GET: api/trustfunds
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrustFundDto>>> GetTrustFunds()
        {
            return await _context.TrustFunds
                .Select(f => new TrustFundDto
                {
                    FundId = f.FundId,
                    PropId = f.PropId,
                    FundTotalValue = f.FundTotalValue,
                    Notes = f.Notes,
                    StartDate = f.StartDate,
                    FundTitle = f.FundTitle,
                    FundTitle1 = f.FundTitle1,
                    CreationDate = f.CreationDate
                })
                .ToListAsync();
        }

        // GET: api/trustfunds/1
        [HttpGet("{id}")]
        public async Task<ActionResult<TrustFundDto>> GetTrustFund(int id)
        {
            var fund = await _context.TrustFunds
                .Where(f => f.FundId == id)
                .Select(f => new TrustFundDto
                {
                    FundId = f.FundId,
                    PropId = f.PropId,
                    FundTotalValue = f.FundTotalValue,
                    Notes = f.Notes,
                    StartDate = f.StartDate,
                    FundTitle = f.FundTitle,
                    FundTitle1 = f.FundTitle1,
                    CreationDate = f.CreationDate
                })
                .FirstOrDefaultAsync();

            if (fund == null)
            {
                return NotFound();
            }

            return fund;
        }
    }
}
