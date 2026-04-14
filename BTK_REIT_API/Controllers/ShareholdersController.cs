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

        // ── PATCH /api/shareholders/{id} ────────────────────────────────────
        /// <summary>
        /// Partial update for a shareholder. Only non-null fields in the body are applied.
        /// Soft-delete: set Status = "inactive" or "suspended".
        /// Blocks full deletion when the shareholder holds an active fund stake (pct_owned > 0, end_date IS NULL).
        /// </summary>
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> PatchShareholder(int id, [FromBody] PatchShareholderDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var shareholder = await _context.Shareholders
                .FirstOrDefaultAsync(s => s.ShId == id);

            if (shareholder is null)
                return NotFound($"Shareholder with sh_id={id} not found.");

            // ── Status guard: cannot deactivate/suspend while holding active stake ──
            if (!string.IsNullOrWhiteSpace(dto.Status) &&
                !string.Equals(dto.Status, "active", StringComparison.OrdinalIgnoreCase))
            {
                bool hasActiveFundStake = await _context.FundDetails
                    .AnyAsync(fd => fd.ShId == id && fd.EndDate == null && fd.PctOwned > 0);

                if (hasActiveFundStake)
                    return Conflict(
                        $"Cannot change status to '{dto.Status}': shareholder sh_id={id} currently holds " +
                        "an active stake in one or more funds. Transfer or retire ownership first.");
            }

            // ── Status validation ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                try
                {
                    var (statusValid, statusAllowed) = await _validation.IsValidAsync("status_sh", dto.Status);
                    if (!statusValid)
                        return BadRequest(new { error = "Invalid value", field = "status", allowed = statusAllowed });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ── ShType validation ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.ShType))
            {
                try
                {
                    var (typeValid, typeAllowed) = await _validation.IsValidAsync("sh_type", dto.ShType);
                    if (!typeValid)
                        return BadRequest(new { error = "Invalid value", field = "sh_type", allowed = typeAllowed });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ── UserName uniqueness ───────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.UserName) &&
                !string.Equals(dto.UserName, shareholder.UserName, StringComparison.OrdinalIgnoreCase))
            {
                bool taken = await _context.Shareholders
                    .AnyAsync(s => s.UserName == dto.UserName && s.ShId != id);
                if (taken)
                    return Conflict($"Username '{dto.UserName}' is already taken.");
            }

            // ── CNIC uniqueness ───────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.Cnic) &&
                !string.Equals(dto.Cnic, shareholder.Cnic, StringComparison.OrdinalIgnoreCase))
            {
                bool cnicTaken = await _context.Shareholders
                    .AnyAsync(s => s.Cnic == dto.Cnic && s.ShId != id);
                if (cnicTaken)
                    return Conflict($"CNIC '{dto.Cnic}' is already registered.");
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string oldSnapshot =
                        $"FullName={shareholder.FullName}, UserName={shareholder.UserName}, " +
                        $"Status={shareholder.Status}, ShType={shareholder.ShType}, " +
                        $"CNIC={shareholder.Cnic ?? "null"}, NTN={shareholder.NtnNo ?? "null"}";

                    if (!string.IsNullOrWhiteSpace(dto.FullName))    shareholder.FullName     = dto.FullName.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.UserName))    shareholder.UserName     = dto.UserName;
                    if (!string.IsNullOrWhiteSpace(dto.ShType))      shareholder.ShType       = dto.ShType.ToLower();
                    if (!string.IsNullOrWhiteSpace(dto.ContactNo))   shareholder.ContactNo    = dto.ContactNo;
                    if (!string.IsNullOrWhiteSpace(dto.ContactEmail)) shareholder.ContactEmail = dto.ContactEmail;
                    if (dto.Cnic is not null)                        shareholder.Cnic         = string.IsNullOrWhiteSpace(dto.Cnic) ? null : dto.Cnic;
                    if (dto.NtnNo is not null)                       shareholder.NtnNo        = string.IsNullOrWhiteSpace(dto.NtnNo) ? null : dto.NtnNo;
                    if (dto.PassportNo is not null)                  shareholder.PassportNo   = string.IsNullOrWhiteSpace(dto.PassportNo) ? null : dto.PassportNo;
                    if (dto.IsFiller.HasValue)                       shareholder.IsFiller     = dto.IsFiller.Value;
                    if (dto.IsOverseas.HasValue)                     shareholder.IsOverseas   = dto.IsOverseas.Value;
                    if (!string.IsNullOrWhiteSpace(dto.Status))      shareholder.Status       = dto.Status.ToLower();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Shareholder",
                        recordId: shareholder.ShId,
                        action: $"PATCH: Shareholder '{shareholder.UserName}' (sh_id={id}) updated.",
                        oldInfo: oldSnapshot);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new ShareholderDto
                    {
                        ShId         = shareholder.ShId,
                        ShType       = shareholder.ShType,
                        UserName     = shareholder.UserName,
                        FullName     = shareholder.FullName,
                        Cnic         = shareholder.Cnic,
                        NtnNo        = shareholder.NtnNo,
                        PassportNo   = shareholder.PassportNo,
                        ContactNo    = shareholder.ContactNo,
                        ContactEmail = shareholder.ContactEmail,
                        IsFiller     = shareholder.IsFiller,
                        IsOverseas   = shareholder.IsOverseas,
                        IsReit       = shareholder.IsReit,
                        CreationDate = shareholder.CreationDate,
                        Status       = shareholder.Status
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Shareholder update failed: {ex.Message}");
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
