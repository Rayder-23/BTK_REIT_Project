using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/payment")]
    [Tags("Payment")]
    public class PaymentController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;

        public PaymentController(ReitContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── POST /api/Payment/update ─────────────────────────────────────────
        /// <summary>
        /// Records an additional payment instalment against an existing pending or partial payment.
        /// Automatically flips status to 'paid' and stamps payment_date when cumulative
        /// amount_paid reaches net_amount_due.
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> UpdatePayment([FromBody] PaymentUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── Fast-fail FK / business rule checks before opening a transaction ──

            // Verify the admin user exists.
            bool adminExists = await _context.AdminUsers
                .AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Load the payment record.
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.PaymentId == dto.PaymentId);

                    if (payment == null)
                        return NotFound($"Payment ID {dto.PaymentId} not found.");

                    // Only pending/partial records can be updated.
                    if (payment.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"Payment ID {dto.PaymentId} is already marked 'paid' and cannot be updated.");

                    // Verify the shareholder linked to this payment is still active.
                    var shareholder = await _context.Shareholders
                        .FirstOrDefaultAsync(s => s.ShId == payment.ShId);

                    if (shareholder == null)
                        return BadRequest($"Shareholder sh_id={payment.ShId} linked to this payment does not exist.");

                    if (!shareholder.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"Shareholder sh_id={payment.ShId} has status '{shareholder.Status}' and is not active.");

                    // Capture old state for audit.
                    string oldAmountPaid = payment.AmountPaid.ToString("F2");
                    string oldStatus     = payment.Status;

                    // Apply the new instalment.
                    decimal newTotalPaid = payment.AmountPaid + dto.AmountReceived;
                    DateOnly today       = DateOnly.FromDateTime(DateTime.Today);

                    payment.AmountPaid = newTotalPaid;

                    if (newTotalPaid >= payment.NetAmountDue)
                    {
                        payment.Status      = "paid";
                        payment.PaymentDate = today;
                    }
                    else
                    {
                        payment.Status = "partial";
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                        payment.Notes = dto.Notes;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Payments",
                        recordId: payment.PaymentId,
                        action: $"UPDATE: Payment ID {payment.PaymentId} — received instalment of " +
                                $"{dto.AmountReceived:F2}. " +
                                $"TotalPaid={newTotalPaid:F2}, NetDue={payment.NetAmountDue:F2}, " +
                                $"NewStatus={payment.Status}" +
                                (payment.Status == "paid" ? $", PaymentDate={today}." : "."),
                        oldInfo: $"AmountPaid={oldAmountPaid}, Status={oldStatus}");

                    await _context.SaveChangesAsync();   // Persists payment update + audit log
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        payment.PaymentId,
                        payment.ShId,
                        payment.FundId,
                        payment.GrossFundAmount,
                        payment.Tax,
                        payment.AdditionalPayments,
                        payment.NetAmountDue,
                        payment.AmountPaid,
                        payment.Status,
                        payment.PaymentDate,
                        Message = payment.Status == "paid"
                            ? "Payment fully settled."
                            : $"Instalment recorded. Outstanding: {(payment.NetAmountDue - newTotalPaid):F2}."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Payment update failed: {ex.Message}");
                }
            });
        }
    }
}
