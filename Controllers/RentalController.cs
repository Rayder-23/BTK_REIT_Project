using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RentalController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;

        public RentalController(ReitContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── POST /api/Rental/record ──────────────────────────────────────────
        /// <summary>
        /// Creates the initial RentalIncome record for a fund/period.
        /// Returns 409 Conflict if a record already exists for the same fund, month, and year.
        /// </summary>
        [HttpPost("record")]
        public async Task<IActionResult> RecordRental([FromBody] RentRecordDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Verify the TrustFund exists.
            bool fundExists = await _context.TrustFunds.AnyAsync(f => f.FundId == dto.FundId);
            if (!fundExists)
                return BadRequest($"TrustFund with fund_id={dto.FundId} does not exist.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Check for UQ_RentalIncome_period uniqueness before inserting.
                    bool periodExists = await _context.RentalIncomes.AnyAsync(ri =>
                        ri.FundId    == dto.FundId &&
                        ri.RentMonth == dto.RentMonth &&
                        ri.RentYear  == dto.RentYear);

                    if (periodExists)
                        return Conflict(
                            $"A RentalIncome record already exists for FundID {dto.FundId}, " +
                            $"{dto.RentMonth}/{dto.RentYear}.");

                    var income = new RentalIncome
                    {
                        FundId    = dto.FundId,
                        RentMonth = dto.RentMonth,
                        RentYear  = dto.RentYear,
                        DueDate   = dto.DueDate,
                        AmountDue = dto.AmountDue,
                        AmountPaid = 0m,
                        LateFee   = 0m,
                        Status    = "overdue",
                        Notes     = dto.Notes
                    };

                    _context.RentalIncomes.Add(income);
                    await _context.SaveChangesAsync();   // Populates income.RentId

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "RentalIncome",
                        recordId: income.RentId,
                        action: $"INSERT: RentalIncome created for FundID {dto.FundId}, " +
                                $"period {dto.RentMonth}/{dto.RentYear}. " +
                                $"AmountDue={dto.AmountDue:F2}, DueDate={dto.DueDate}, Status=overdue.");

                    await _context.SaveChangesAsync();   // Persists audit log
                    await transaction.CommitAsync();

                    return CreatedAtAction(null, null, new
                    {
                        income.RentId,
                        income.FundId,
                        income.RentMonth,
                        income.RentYear,
                        income.DueDate,
                        income.AmountDue,
                        income.AmountPaid,
                        income.LateFee,
                        income.Status
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to record rental income: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/Rental/receive-payment/{id} ───────────────────────────
        /// <summary>
        /// Records rent arrival against an existing RentalIncome record.
        /// Sets status to 'paid' when amountPaid >= amountDue, otherwise 'partial'.
        /// </summary>
        [HttpPatch("receive-payment/{id:int}")]
        public async Task<IActionResult> ReceivePayment(int id, [FromBody] RentPaymentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var income = await _context.RentalIncomes
                        .FirstOrDefaultAsync(ri => ri.RentId == id);

                    if (income == null)
                        return NotFound($"RentalIncome record with rent_id={id} not found.");

                    // Already fully paid — prevent overwrite.
                    if (income.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"RentalIncome rent_id={id} is already marked 'paid'.");

                    // Capture old state for audit.
                    string  oldStatus     = income.Status;
                    decimal oldAmountPaid = income.AmountPaid;
                    decimal oldLateFee    = income.LateFee;

                    // Apply the payment.
                    income.AmountPaid  = dto.AmountPaid;
                    income.LateFee     = dto.LateFee;
                    income.PaymentDate = dto.PaymentDate;
                    income.Status      = dto.AmountPaid >= income.AmountDue ? "paid" : "partial";

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                        income.Notes = dto.Notes;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "RentalIncome",
                        recordId: income.RentId,
                        action: $"UPDATE: Payment received for RentalIncome rent_id={id}. " +
                                $"AmountPaid={dto.AmountPaid:F2}, LateFee={dto.LateFee:F2}, " +
                                $"PaymentDate={dto.PaymentDate}, NewStatus={income.Status}.",
                        oldInfo: $"Status={oldStatus}, AmountPaid={oldAmountPaid:F2}, LateFee={oldLateFee:F2}");

                    await _context.SaveChangesAsync();   // Persists income update + audit log
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        income.RentId,
                        income.FundId,
                        income.RentMonth,
                        income.RentYear,
                        income.AmountDue,
                        income.AmountPaid,
                        income.LateFee,
                        income.PaymentDate,
                        income.Status
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to record rental payment: {ex.Message}");
                }
            });
        }
    }
}
