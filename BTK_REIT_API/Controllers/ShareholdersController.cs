using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/shareholders")]
    [Tags("Shareholders")]
    public class ShareholdersController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public ShareholdersController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
        }

        // ── GET /api/shareholders ────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShareholderDto>>> GetShareholders()
        {
            return await _context.Shareholders
                .Select(s => new ShareholderDto
                {
                    ShId         = s.ShId,
                    ShType       = s.ShType,
                    UserName     = s.UserName,
                    FullName     = s.FullName,
                    Cnic         = s.Cnic,
                    NtnNo        = s.NtnNo,
                    PassportNo   = s.PassportNo,
                    ContactNo    = s.ContactNo,
                    ContactEmail = s.ContactEmail,
                    IsFiller     = s.IsFiller,
                    IsOverseas   = s.IsOverseas,
                    IsReit       = s.IsReit,
                    CreationDate = s.CreationDate,
                    Status       = s.Status
                })
                .ToListAsync();
        }

        // ── GET /api/shareholders/{id} ───────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ShareholderDto>> GetShareholder(int id)
        {
            var shareholder = await _context.Shareholders
                .Where(s => s.ShId == id)
                .Select(s => new ShareholderDto
                {
                    ShId         = s.ShId,
                    ShType       = s.ShType,
                    UserName     = s.UserName,
                    FullName     = s.FullName,
                    Cnic         = s.Cnic,
                    NtnNo        = s.NtnNo,
                    PassportNo   = s.PassportNo,
                    ContactNo    = s.ContactNo,
                    ContactEmail = s.ContactEmail,
                    IsFiller     = s.IsFiller,
                    IsOverseas   = s.IsOverseas,
                    IsReit       = s.IsReit,
                    CreationDate = s.CreationDate,
                    Status       = s.Status
                })
                .FirstOrDefaultAsync();

            if (shareholder == null)
                return NotFound();

            return shareholder;
        }

        // ── GET /api/shareholders/{id}/portfolio ─────────────────────────────
        [HttpGet("{id:int}/portfolio")]
        public async Task<IActionResult> GetPortfolio(int id)
        {
            bool exists = await _context.Shareholders.AnyAsync(s => s.ShId == id);
            if (!exists)
                return NotFound($"Shareholder with sh_id={id} not found.");

            var portfolio = await _context.FundDetails
                .AsNoTracking()
                .Where(fd => fd.ShId == id && fd.EndDate == null)
                .Select(fd => new ShareholderPortfolioItemDto
                {
                    FundId       = fd.FundId,
                    FundTitle    = fd.Fund.FundTitle ?? fd.Fund.FundTitle1 ?? "",
                    PctOwned     = fd.PctOwned,
                    ShareValue   = fd.ShareValue,
                    AcquiredDate = fd.AcquiredDate
                })
                .ToListAsync();

            return Ok(portfolio);
        }

        // ── POST /api/shareholders ───────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> RegisterShareholder([FromBody] ShareholderCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var (isValid, allowed) = await _validation.IsValidAsync("sh_type", dto.ShType);
                if (!isValid)
                    return BadRequest(new { error = "Invalid value", field = "sh_type", allowed });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    bool userNameTaken = await _context.Shareholders
                        .AnyAsync(s => s.UserName == dto.UserName);
                    if (userNameTaken)
                        return Conflict($"Username '{dto.UserName}' is already taken.");

                    if (!string.IsNullOrWhiteSpace(dto.Cnic))
                    {
                        bool cnicTaken = await _context.Shareholders
                            .AnyAsync(s => s.Cnic == dto.Cnic);
                        if (cnicTaken)
                            return Conflict($"CNIC '{dto.Cnic}' is already registered.");
                    }

                    var shareholder = new Shareholder
                    {
                        ShType       = dto.ShType.ToLower(),
                        UserName     = dto.UserName,
                        FullName     = dto.FullName,
                        ContactNo    = dto.ContactNo,
                        ContactEmail = dto.ContactEmail,
                        Password     = dto.Password,
                        Cnic         = string.IsNullOrWhiteSpace(dto.Cnic) ? null : dto.Cnic,
                        NtnNo        = dto.NtnNo,
                        PassportNo   = dto.PassportNo,
                        IsFiller     = dto.IsFiller,
                        IsOverseas   = dto.IsOverseas,
                        IsReit       = dto.IsReit,
                        Status       = "active"
                    };

                    _context.Shareholders.Add(shareholder);
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Shareholder",
                        recordId: shareholder.ShId,
                        action: $"New Shareholder Registered: {shareholder.UserName}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(GetShareholder),
                        routeValues: new { id = shareholder.ShId },
                        value: new { shareholder.ShId, shareholder.UserName, shareholder.FullName });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Registration failed: {ex.Message}");
                }
            });
        }

        // ── POST /api/shareholders/account ───────────────────────────────────
        [HttpPost("account")]
        public async Task<IActionResult> AddBankAccount([FromBody] BankAccountCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    bool shareholderExists = await _context.Shareholders
                        .AnyAsync(s => s.ShId == dto.ShId);
                    if (!shareholderExists)
                        return NotFound($"Shareholder with ID {dto.ShId} not found.");

                    bool accountExists = await _context.Shbkaccounts
                        .AnyAsync(a => a.ShId == dto.ShId && a.AcNo == dto.AcNo);
                    if (accountExists)
                        return Conflict($"Account number '{dto.AcNo}' is already linked to ShareholderID {dto.ShId}.");

                    var account = new Shbkaccount
                    {
                        ShId         = dto.ShId,
                        Bank         = dto.Bank,
                        AccountTitle = dto.AccountTitle,
                        AcNo         = dto.AcNo,
                        Status       = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status,
                        ApprovedBy   = dto.UserId,
                        Notes        = dto.Notes
                    };

                    _context.Shbkaccounts.Add(account);
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "SHBKAccounts",
                        recordId: account.ShAccountId,
                        action: $"Bank Account Added for ShareholderID: {dto.ShId}. Bank: {dto.Bank}, Account: {dto.AcNo}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(GetShareholder),
                        routeValues: new { id = dto.ShId },
                        value: new { account.ShAccountId, account.ShId, account.Bank, account.AcNo, account.Status });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Bank account linking failed: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/shareholders/{id}/status ──────────────────────────────
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateShareholderStatusDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            try
            {
                var (isValid, allowed) = await _validation.IsValidAsync("status_sh", dto.Status);
                if (!isValid)
                    return BadRequest(new { error = "Invalid value", field = "status", allowed });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var shareholder = await _context.Shareholders
                        .FirstOrDefaultAsync(s => s.ShId == id);

                    if (shareholder == null)
                        return NotFound($"Shareholder with sh_id={id} not found.");

                    string oldStatus = shareholder.Status;
                    string newStatus = dto.Status.ToLower();

                    if (string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
                        return Ok(new
                        {
                            shareholder.ShId,
                            shareholder.Status,
                            Action  = "no-change",
                            Message = $"Shareholder sh_id={id} is already '{newStatus}'."
                        });

                    shareholder.Status = newStatus;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Shareholder",
                        recordId: shareholder.ShId,
                        action: $"UPDATE: Shareholder sh_id={id} status changed to '{newStatus}'.",
                        oldInfo: $"Status={oldStatus}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        shareholder.ShId,
                        shareholder.FullName,
                        OldStatus = oldStatus,
                        NewStatus = shareholder.Status,
                        Action    = "updated"
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Status update failed: {ex.Message}");
                }
            });
        }
    }
}
