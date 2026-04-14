using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/config")]
    [Tags("Config")]
    public class ConfigController : ControllerBase
    {
        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IMemoryCache _cache;

        public ConfigController(ReitContext context, IAuditService audit, IMemoryCache cache)
        {
            _context = context;
            _audit   = audit;
            _cache   = cache;
        }

        // Cache key must match the format used in ValidationService.
        private static string CacheKey(string key) => $"config_validation:{key.ToLower().Trim()}";

        // ── GET /api/Config/grouped ──────────────────────────────────────────
        /// <summary>
        /// Returns all configuration entries grouped by key, including inactive ones.
        /// Each entry aggregates the CSV value into a parsed list. Used by the
        /// Configurations management page.
        /// </summary>
        [HttpGet("grouped")]
        public async Task<IActionResult> GetGrouped()
        {
            var all = await _context.Configurations
                .OrderBy(c => c.Key)
                .ThenByDescending(c => c.IsActive)
                .ToListAsync();

            var grouped = all
                .GroupBy(c => c.Key)
                .Select(g =>
                {
                    // Prefer the active row; fall back to the most-recently-edited
                    var representative = g.FirstOrDefault(c => c.IsActive)
                                     ?? g.OrderByDescending(c => c.LastEdited).First();

                    // Merge all values from the group (handles legacy multi-row keys)
                    var allValues = g
                        .SelectMany(c => c.Value
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(v => v.Trim())
                            .Where(v => v.Length > 0))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(v => v)
                        .ToList();

                    return new ConfigGroupedDto
                    {
                        ConfigId   = representative.ConfigId,
                        Key        = representative.Key,
                        Value      = string.Join(", ", allValues),
                        Values     = allValues,
                        IsActive   = representative.IsActive,
                        Notes      = representative.Notes,
                        LastEdited = representative.LastEdited,
                        UserId     = representative.UserId
                    };
                })
                .OrderBy(g => g.Key)
                .ToList();

            return Ok(grouped);
        }

        // ── PATCH /api/Config/{id}/toggle ────────────────────────────────────
        /// <summary>
        /// Toggles the is_active flag for a configuration row identified by its
        /// config_id. Invalidates the ValidationService cache for that key.
        /// </summary>
        [HttpPatch("{id:int}/toggle")]
        public async Task<IActionResult> ToggleActive(int id, [FromQuery] int userId)
        {
            if (userId <= 0)
                return BadRequest("userId query parameter is required.");

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == userId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={userId} does not exist.");

            var config = await _context.Configurations.FindAsync(id);
            if (config == null)
                return NotFound($"Configuration with id={id} not found.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    bool wasActive = config.IsActive;
                    config.IsActive   = !wasActive;
                    config.LastEdited = DateTime.UtcNow;
                    config.UserId     = userId;

                    _audit.LogAction(
                        userId: userId,
                        tableName: "Configurations",
                        recordId: config.ConfigId,
                        action: $"TOGGLE: Configuration key '{config.Key}' set IsActive={config.IsActive}.",
                        oldInfo: $"IsActive={wasActive}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Invalidate cache so ValidationService picks up the change.
                    _cache.Remove(CacheKey(config.Key));

                    return Ok(new
                    {
                        config.ConfigId,
                        config.Key,
                        config.IsActive,
                        Action  = config.IsActive ? "activated" : "deactivated",
                        Message = $"Configuration '{config.Key}' is now {(config.IsActive ? "active" : "inactive")}."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to toggle configuration: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/Config/{id}/notes ─────────────────────────────────────
        /// <summary>
        /// Updates the notes field for a configuration row by its config_id.
        /// </summary>
        [HttpPatch("{id:int}/notes")]
        public async Task<IActionResult> UpdateNotes(int id, [FromBody] ConfigPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var config = await _context.Configurations.FindAsync(id);
            if (config == null)
                return NotFound($"Configuration with id={id} not found.");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    string? oldNotes = config.Notes;
                    config.Notes      = dto.Value;
                    config.LastEdited = DateTime.UtcNow;
                    config.UserId     = dto.UserId;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Configurations",
                        recordId: config.ConfigId,
                        action: $"UPDATE: Notes updated for key '{config.Key}'.",
                        oldInfo: $"Notes={oldNotes}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { config.ConfigId, config.Key, config.Notes });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to update notes: {ex.Message}");
                }
            });
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

        // ── PATCH /api/Config/append/{key} ──────────────────────────────────
        /// <summary>
        /// Appends a single value token to the comma-separated list stored under
        /// the given key. Idempotent: if the value already exists the record is
        /// not modified and 200 is returned immediately.
        /// Invalidates the ValidationService cache entry for the key.
        /// </summary>
        [HttpPatch("append/{key}")]
        public async Task<IActionResult> Append(string key, [FromBody] ConfigPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var config = await _context.Configurations
                .FirstOrDefaultAsync(c => c.Key == key && c.IsActive);

            if (config == null)
                return NotFound($"Active configuration key '{key}' not found.");

            // Split, trim, deduplicate — preserve original casing of existing tokens.
            var tokens = config.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToList();

            string newToken = dto.Value.Trim();

            // Idempotency check — case-insensitive.
            bool alreadyExists = tokens.Any(t =>
                string.Equals(t, newToken, StringComparison.OrdinalIgnoreCase));

            if (alreadyExists)
                return Ok(new
                {
                    config.ConfigId,
                    config.Key,
                    config.Value,
                    Action  = "no-change",
                    Message = $"'{newToken}' already exists in key '{key}'. No update performed."
                });

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    string oldValue = config.Value;

                    tokens.Add(newToken);
                    config.Value      = string.Join(", ", tokens);
                    config.LastEdited = DateTime.UtcNow;
                    config.UserId     = dto.UserId;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Configurations",
                        recordId: config.ConfigId,
                        action: $"UPDATE: Appended '{newToken}' to key '{key}'.",
                        oldInfo: $"Value={oldValue}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Invalidate the ValidationService cache so the next request re-reads from DB.
                    _cache.Remove(CacheKey(key));

                    return Ok(new
                    {
                        config.ConfigId,
                        config.Key,
                        config.Value,
                        Action  = "appended",
                        Message = $"'{newToken}' appended to key '{key}'."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to append to configuration: {ex.Message}");
                }
            });
        }

        // ── PATCH /api/Config/remove/{key} ──────────────────────────────────
        /// <summary>
        /// Removes a single value token from the comma-separated list stored under
        /// the given key. Idempotent: if the value does not exist 200 is returned
        /// without modifying the record.
        /// Invalidates the ValidationService cache entry for the key.
        /// </summary>
        [HttpPatch("remove/{key}")]
        public async Task<IActionResult> Remove(string key, [FromBody] ConfigPatchDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            bool adminExists = await _context.AdminUsers.AnyAsync(a => a.UserId == dto.UserId);
            if (!adminExists)
                return BadRequest($"AdminUser with user_id={dto.UserId} does not exist.");

            var config = await _context.Configurations
                .FirstOrDefaultAsync(c => c.Key == key && c.IsActive);

            if (config == null)
                return NotFound($"Active configuration key '{key}' not found.");

            var tokens = config.Value
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(v => v.Trim())
                .ToList();

            string targetToken = dto.Value.Trim();

            // Case-insensitive removal — remove all matching tokens (defensive against duplicates).
            var remaining = tokens
                .Where(t => !string.Equals(t, targetToken, StringComparison.OrdinalIgnoreCase))
                .ToList();

            bool wasPresent = remaining.Count < tokens.Count;

            if (!wasPresent)
                return Ok(new
                {
                    config.ConfigId,
                    config.Key,
                    config.Value,
                    Action  = "no-change",
                    Message = $"'{targetToken}' was not found in key '{key}'. No update performed."
                });

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    string oldValue = config.Value;

                    // Guard: never persist an empty CSV (would break all downstream validation).
                    if (remaining.Count == 0)
                        return BadRequest(
                            $"Cannot remove '{targetToken}': it is the only value in key '{key}'. " +
                            "Use DELETE /api/Config/{key} to disable the key entirely.");

                    config.Value      = string.Join(", ", remaining);
                    config.LastEdited = DateTime.UtcNow;
                    config.UserId     = dto.UserId;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Configurations",
                        recordId: config.ConfigId,
                        action: $"UPDATE: Removed '{targetToken}' from key '{key}'.",
                        oldInfo: $"Value={oldValue}");

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Invalidate the ValidationService cache so the next request re-reads from DB.
                    _cache.Remove(CacheKey(key));

                    return Ok(new
                    {
                        config.ConfigId,
                        config.Key,
                        config.Value,
                        Action  = "removed",
                        Message = $"'{targetToken}' removed from key '{key}'."
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Failed to remove from configuration: {ex.Message}");
                }
            });
        }
    }
}
