using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/trustfunds")]
    [Tags("Trust Funds")]
    public class TrustFundsController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public TrustFundsController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
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


        // ── GET /api/trustfunds ───────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrustFundDto>>> GetTrustFunds()
        {
            var funds = await _context.TrustFunds.ToListAsync();
            return funds.Select(MapToDto).ToList();
        }

        // ── GET /api/trustfunds/{id} ─────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<ActionResult<TrustFundDto>> GetTrustFund(int id)
        {
            var fund = await _context.TrustFunds.FindAsync(id);
            if (fund is null)
                return NotFound();
            return MapToDto(fund);
        }

        // ── PATCH /api/trustfunds/{id} ───────────────────────────────────────
        /// <summary>
        /// Partial update: FundTitle (long/legal), FundTitle1 (short/identifier),
        /// fund_total_value, status, and/or notes.
        /// Status is validated against the 'fund_status' config key and stored
        /// as a STATUS: prefix in Notes (no schema migration required).
        ///
        /// Schema reminder:
        ///   C# FundTitle  → DB column FundTitle  (long/legal name, no HasColumnName)
        ///   C# FundTitle1 → DB column fund_title  (short/identifier name)
        /// </summary>
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> PatchTrustFund(int id, [FromBody] PatchTrustFundDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var fund = await _context.TrustFunds.FindAsync(id);
            if (fund is null)
                return NotFound($"TrustFund with fund_id={id} not found.");

            // Validate status if provided
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                try
                {
                    var (valid, allowed) = await _validation.IsValidAsync("fund_status", dto.Status);
                    if (!valid)
                        return BadRequest(new { error = "Invalid status", field = "status", allowed });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            string oldSnapshot =
                $"FundTitle={fund.FundTitle}, FundTitle1={fund.FundTitle1}, " +
                $"FundTotalValue={fund.FundTotalValue:F2}, Notes={fund.Notes}";

            // Long/legal name
            if (!string.IsNullOrWhiteSpace(dto.FundTitle))
                fund.FundTitle = dto.FundTitle.Trim();

            // Short/identifier name (DB column: fund_title)
            if (!string.IsNullOrWhiteSpace(dto.FundTitle1))
                fund.FundTitle1 = dto.FundTitle1.Trim();

            if (dto.FundTotalValue.HasValue && dto.FundTotalValue.Value > 0)
                fund.FundTotalValue = dto.FundTotalValue.Value;

            // Status stored as STATUS:<value> prefix; preserve any user notes after the pipe
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                string statusToken  = $"STATUS:{dto.Status.Trim().ToLower()}";
                string? userNotes   = ExtractUserNotes(fund.Notes);
                fund.Notes = string.IsNullOrWhiteSpace(userNotes)
                    ? statusToken
                    : $"{statusToken}|{userNotes}";
            }
            else if (dto.Notes is not null)
            {
                // Preserve existing STATUS prefix, only update the user-notes portion
                string? existingStatus = ExtractStatus(fund.Notes);
                string? newUserNotes   = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
                fund.Notes = existingStatus is not null
                    ? (newUserNotes is not null ? $"STATUS:{existingStatus}|{newUserNotes}" : $"STATUS:{existingStatus}")
                    : newUserNotes;
            }

            _audit.LogAction(
                userId:     dto.UserId,
                tableName:  "TrustFund",
                recordId:   fund.FundId,
                action:     $"PATCH: TrustFund (fund_id={id}) updated.",
                oldInfo:    oldSnapshot);

            await _context.SaveChangesAsync();
            return Ok(MapToDto(fund));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Extracts the status value from a Notes string formatted as
        /// "STATUS:active" or "STATUS:active|some user note".
        /// Returns null if no STATUS prefix exists.
        /// </summary>
        private static string? ExtractStatus(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes) || !notes.StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase))
                return null;
            var afterPrefix = notes.Substring(7);
            var pipeIdx = afterPrefix.IndexOf('|');
            return pipeIdx >= 0 ? afterPrefix[..pipeIdx].Trim() : afterPrefix.Trim();
        }

        /// <summary>
        /// Extracts the user-written notes portion from a Notes string,
        /// stripping any leading "STATUS:xxx|" prefix.
        /// </summary>
        private static string? ExtractUserNotes(string? notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return null;
            if (!notes.StartsWith("STATUS:", StringComparison.OrdinalIgnoreCase)) return notes;
            var pipeIdx = notes.IndexOf('|');
            return pipeIdx >= 0 ? notes[(pipeIdx + 1)..].Trim() : null;
        }

        /// <summary>
        /// Maps a TrustFund entity to TrustFundDto, correctly resolving Status
        /// and separating the Notes user text from the STATUS prefix.
        /// </summary>
        private static TrustFundDto MapToDto(TrustFund f) => new()
        {
            FundId         = f.FundId,
            PropId         = f.PropId,
            FundTotalValue = f.FundTotalValue,
            // Expose clean user notes (no STATUS: prefix) to the UI
            Notes          = ExtractUserNotes(f.Notes),
            StartDate      = f.StartDate,
            // FundTitle  = long/legal name (DB column FundTitle, no HasColumnName override)
            FundTitle      = f.FundTitle,
            // FundTitle1 = short/identifier name (DB column fund_title)
            FundTitle1     = f.FundTitle1,
            CreationDate   = f.CreationDate,
            Status         = ExtractStatus(f.Notes)
        };
    }
}
