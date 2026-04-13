using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/expense")]
    [Tags("Expense")]
    public class ExpenseController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public ExpenseController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
        }

        // ── POST /api/Expense/record ─────────────────────────────────────────
        /// <summary>
        /// Creates a new expense record with status 'pending'.
        /// </summary>
        [HttpPost("record")]
        public async Task<IActionResult> RecordExpense([FromBody] CreateExpenseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── Fast-fail business rule checks ───────────────────────────────
            try
            {
                var (etValid, etAllowed) = await _validation.IsValidAsync("expense_type", dto.ExpenseType);
                if (!etValid)
                    return BadRequest(new { error = "Invalid value", field = "expense_type", allowed = etAllowed });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            bool fundExists = await _context.TrustFunds.AnyAsync(f => f.FundId == dto.FundId);
            if (!fundExists)
                return BadRequest($"TrustFund with fund_id={dto.FundId} does not exist.");

            // Verify the admin user exists (FK: approved_by → AdminUsers).
            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            // If PaidBy is supplied, verify the shareholder is active.
            if (dto.PaidBy.HasValue)
            {
                var payer = await _context.Shareholders
                    .FirstOrDefaultAsync(s => s.ShId == dto.PaidBy.Value);

                if (payer == null)
                    return BadRequest($"Shareholder with sh_id={dto.PaidBy.Value} does not exist.");

                if (!payer.Status.Equals("active", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(
                        $"Shareholder sh_id={dto.PaidBy.Value} has status '{payer.Status}' and is not active.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var expense = new Expense
                    {
                        FundId       = dto.FundId,
                        Month        = dto.Month,
                        Year         = dto.Year,
                        ExpenseType  = dto.ExpenseType.ToLower(),
                        Description  = dto.Description,
                        Amount       = dto.Amount,
                        PaidBy       = dto.PaidBy,
                        Status       = "pending",
                        ApprovedBy   = dto.UserId,
                        CreationDate = DateOnly.FromDateTime(DateTime.Today),
                        Notes        = dto.Notes
                    };

                    _context.Expenses.Add(expense);
                    await _context.SaveChangesAsync();   // Populates expense.ExpenseId

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Expenses",
                        recordId: expense.ExpenseId,
                        action: $"INSERT: Expense recorded for FundID {dto.FundId}, " +
                                $"period {dto.Month}/{dto.Year}. " +
                                $"Type={expense.ExpenseType}, Amount={dto.Amount:F2}, Status=pending.");

                    await _context.SaveChangesAsync();   // Persists audit log
                    await transaction.CommitAsync();

                    return CreatedAtAction(null, null, new
                    {
                        expense.ExpenseId,
                        expense.FundId,
                        expense.Month,
                        expense.Year,
                        expense.ExpenseType,
                        expense.Description,
                        expense.Amount,
                        expense.Status,
                        expense.CreationDate
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(409, $"Database constraint violation: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to record expense: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/Expense/settle/{id} ───────────────────────────────────
        /// <summary>
        /// Marks an expense as paid. Sets paid_on and status = 'paid'.
        /// </summary>
        [HttpPatch("settle/{id:int}")]
        public async Task<IActionResult> SettleExpense(int id, [FromBody] SettleExpenseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var expense = await _context.Expenses
                        .FirstOrDefaultAsync(e => e.ExpenseId == id);

                    if (expense == null)
                        return NotFound($"Expense with expense_id={id} not found.");

                    if (expense.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                        return BadRequest($"Expense expense_id={id} is already marked 'paid'.");

                    // Capture before state for audit.
                    string  oldStatus = expense.Status;
                    string  oldNotes  = expense.Notes ?? "";
                    DateOnly paidOn   = dto.PaidOn ?? DateOnly.FromDateTime(DateTime.Today);

                    expense.Status = "paid";
                    expense.PaidOn = paidOn;

                    if (!string.IsNullOrWhiteSpace(dto.Notes))
                        expense.Notes = dto.Notes;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Expenses",
                        recordId: expense.ExpenseId,
                        action: $"UPDATE: Expense expense_id={id} settled. " +
                                $"NewStatus=paid, PaidOn={paidOn}.",
                        oldInfo: $"Status={oldStatus}, PaidOn=null, Notes={oldNotes}");

                    await _context.SaveChangesAsync();   // Persists expense update + audit log
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        expense.ExpenseId,
                        expense.FundId,
                        expense.ExpenseType,
                        expense.Amount,
                        expense.Status,
                        expense.PaidOn
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(409, $"Database constraint violation: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to settle expense: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/Expense/dispute/{id} ──────────────────────────────────
        /// <summary>
        /// Marks an expense as disputed and records the reason in Notes.
        /// </summary>
        [HttpPatch("dispute/{id:int}")]
        public async Task<IActionResult> DisputeExpense(int id, [FromBody] DisputeExpenseDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var expense = await _context.Expenses
                        .FirstOrDefaultAsync(e => e.ExpenseId == id);

                    if (expense == null)
                        return NotFound($"Expense with expense_id={id} not found.");

                    if (expense.Status.Equals("disputed", StringComparison.OrdinalIgnoreCase))
                        return BadRequest($"Expense expense_id={id} is already marked 'disputed'.");

                    if (expense.Status.Equals("paid", StringComparison.OrdinalIgnoreCase))
                        return BadRequest(
                            $"Expense expense_id={id} is already 'paid' and cannot be disputed.");

                    string oldStatus = expense.Status;
                    string oldNotes  = expense.Notes ?? "";

                    expense.Status = "disputed";
                    expense.Notes  = dto.Reason;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Expenses",
                        recordId: expense.ExpenseId,
                        action: $"UPDATE: Expense expense_id={id} marked as disputed.",
                        oldInfo: $"Status={oldStatus}, Notes={oldNotes}");

                    await _context.SaveChangesAsync();   // Persists expense update + audit log
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        expense.ExpenseId,
                        expense.FundId,
                        expense.ExpenseType,
                        expense.Amount,
                        expense.Status,
                        expense.Notes
                    });
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(409, $"Database constraint violation: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to dispute expense: {ex.Message}");
                }
            });
        }
    }
}
