using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareholderController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public ShareholderController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
        }

        // POST: api/Shareholder
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
                    // Enforce unique userName at the application layer for a clear 409 response.
                    bool userNameTaken = await _context.Shareholders
                        .AnyAsync(s => s.UserName == dto.UserName);
                    if (userNameTaken)
                        return Conflict($"Username '{dto.UserName}' is already taken.");

                    // Enforce unique CNIC only when a value is supplied.
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
                        // CreationDate: omitted — DB default (getdate()) supplies it
                    };

                    _context.Shareholders.Add(shareholder);

                    // Flush to DB so that shareholder.ShId is populated by SQL Server identity.
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Shareholder",
                        recordId: shareholder.ShId,
                        action: $"New Shareholder Registered: {shareholder.UserName}");

                    // Persist the audit log entry.
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(ShareholdersController.GetShareholder),
                        controllerName: "Shareholders",
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

        // POST: api/Shareholder/account
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
                    // Verify the shareholder exists.
                    bool shareholderExists = await _context.Shareholders
                        .AnyAsync(s => s.ShId == dto.ShId);
                    if (!shareholderExists)
                        return NotFound($"Shareholder with ID {dto.ShId} not found.");

                    // Enforce the unique constraint on (sh_id + acNo) at the application layer.
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
                        // CreationDate: omitted — DB default (getdate()) supplies it
                    };

                    _context.Shbkaccounts.Add(account);

                    // Flush to get the new ShAccountId before logging.
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "SHBKAccounts",
                        recordId: account.ShAccountId,
                        action: $"Bank Account Added for ShareholderID: {dto.ShId}. Bank: {dto.Bank}, Account: {dto.AcNo}");

                    // Persist the audit log entry.
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(ShareholdersController.GetShareholder),
                        controllerName: "Shareholders",
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
    }
}
