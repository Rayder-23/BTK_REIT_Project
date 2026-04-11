using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConfigController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;

        public ConfigController(ReitContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // ── GET /api/Config ──────────────────────────────────────────────────
        /// <summary>
        /// Returns all active configuration entries. Used to feed UI dropdowns.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var configs = await _context.Configurations
                .Where(c => c.IsActive)
                .OrderBy(c => c.Key)
                .Select(c => new ConfigurationDto
                {
                    ConfigId    = c.ConfigId,
                    Key         = c.Key,
                    Value       = c.Value,
                    IsActive    = c.IsActive,
                    UserId      = c.UserId,
                    LastEdited  = c.LastEdited,
                    Notes       = c.Notes
                })
                .ToListAsync();

            return Ok(configs);
        }

        // ── POST /api/Config/set ─────────────────────────────────────────────
        /// <summary>
        /// Upserts a configuration key/value pair.
        /// Inserts if the key does not exist; updates value and last_edited if it does.
        /// Reactivates a previously soft-deleted key if found inactive.
        /// </summary>
        [HttpPost("set")]
        public async Task<IActionResult> Set([FromBody] ConfigSetDto dto)
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
                    // Look up by key regardless of is_active so we can reactivate soft-deleted entries.
                    var existing = await _context.Configurations
                        .FirstOrDefaultAsync(c => c.Key == dto.Key);

                    if (existing == null)
                    {
                        // ── INSERT ───────────────────────────────────────────
                        var config = new Configuration
                        {
                            Key        = dto.Key,
                            Value      = dto.Value,
                            IsActive   = true,
                            UserId     = dto.UserId,
                            LastEdited = DateTime.UtcNow,
                            Notes      = dto.Notes
                        };

                        _context.Configurations.Add(config);
                        await _context.SaveChangesAsync();   // Populates config.ConfigId

                        _audit.LogAction(
                            userId: dto.UserId,
                            tableName: "Configurations",
                            recordId: config.ConfigId,
                            action: $"INSERT: Configuration key '{dto.Key}' created with value '{dto.Value}'.");

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return CreatedAtAction(nameof(GetAll), null, new
                        {
                            config.ConfigId,
                            config.Key,
                            config.Value,
                            config.IsActive,
                            config.LastEdited,
                            Action = "created"
                        });
                    }
                    else
                    {
                        // ── UPDATE ───────────────────────────────────────────
                        string oldValue    = existing.Value;
                        bool   wasInactive = !existing.IsActive;

                        existing.Value      = dto.Value;
                        existing.IsActive   = true;           // Reactivate if previously soft-deleted
                        existing.UserId     = dto.UserId;
                        existing.LastEdited = DateTime.UtcNow;

                        if (!string.IsNullOrWhiteSpace(dto.Notes))
                            existing.Notes = dto.Notes;

                        string actionDetail = wasInactive
                            ? $"UPDATE: Configuration key '{dto.Key}' reactivated. " +
                              $"Value set to '{dto.Value}'."
                            : $"UPDATE: Configuration key '{dto.Key}' value changed.";

                        _audit.LogAction(
                            userId: dto.UserId,
                            tableName: "Configurations",
                            recordId: existing.ConfigId,
                            action: actionDetail,
                            oldInfo: $"Value={oldValue}, IsActive={!wasInactive}");

                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return Ok(new
                        {
                            existing.ConfigId,
                            existing.Key,
                            existing.Value,
                            existing.IsActive,
                            existing.LastEdited,
                            Action = wasInactive ? "reactivated" : "updated"
                        });
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    await transaction.RollbackAsync();
                    // UQ_Configurations_key violation — race condition between the AnyAsync check and insert.
                    return Conflict(
                        $"A configuration with key '{dto.Key}' already exists (constraint violation). " +
                        $"Detail: {dbEx.InnerException?.Message ?? dbEx.Message}");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to set configuration: {ex.Message}");
                }
            });
        }

        // ── DELETE /api/Config/{key} ─────────────────────────────────────────
        /// <summary>
        /// Soft-deletes a configuration entry by setting is_active = false.
        /// The row is never physically removed.
        /// </summary>
        [HttpDelete("{key}")]
        public async Task<IActionResult> Disable(string key, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("userId query parameter is required.");

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == userId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={userId} does not exist.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var config = await _context.Configurations
                        .FirstOrDefaultAsync(c => c.Key == key);

                    if (config == null)
                        return NotFound($"Configuration key '{key}' not found.");

                    if (!config.IsActive)
                        return BadRequest($"Configuration key '{key}' is already inactive.");

                    config.IsActive   = false;
                    config.LastEdited = DateTime.UtcNow;

                    _audit.LogAction(
                        userId: userId,
                        tableName: "Configurations",
                        recordId: config.ConfigId,
                        action: $"UPDATE: Configuration key '{key}' disabled (soft-delete).",
                        oldInfo: $"IsActive=true, Value={config.Value}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new
                    {
                        config.ConfigId,
                        config.Key,
                        config.IsActive,
                        Message = $"Configuration key '{key}' has been disabled."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to disable configuration: {ex.Message}");
                }
            });
        }
    }
}
