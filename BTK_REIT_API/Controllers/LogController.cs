using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/log")]
    [Tags("Log")]
    public class LogController : ControllerBase
    {
        private readonly ReitContext _context;

        public LogController(ReitContext context)
        {
            _context = context;
        }

        // ── GET /api/log/recent ──────────────────────────────────────────────
        /// <summary>
        /// Returns audit log entries ordered by creation_date descending.
        /// Optional filters: tableName, userId, search (ActionDetails contains),
        /// dateFrom, dateTo, limit (default 200, max 1000).
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent(
            [FromQuery] string?   tableName,
            [FromQuery] int?      userId,
            [FromQuery] string?   search,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int       limit = 200)
        {
            if (limit <= 0 || limit > 1000)
                return BadRequest("limit must be between 1 and 1000.");

            var query = _context.Logs
                .AsNoTracking()
                .Include(l => l.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tableName))
                query = query.Where(l => l.TableName == tableName);

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l => l.ActionDetails.Contains(search));

            if (dateFrom.HasValue)
                query = query.Where(l => l.CreationDate >= dateFrom.Value);

            if (dateTo.HasValue)
            {
                // Include the entire dateTo day
                var endOfDay = dateTo.Value.Date.AddDays(1);
                query = query.Where(l => l.CreationDate < endOfDay);
            }

            var logs = await query
                .OrderByDescending(l => l.CreationDate)
                .Take(limit)
                .Select(l => new LogDto
                {
                    LogId         = l.LogId,
                    UserId        = l.UserId,
                    UserName      = l.User.UserName,
                    TableName     = l.TableName,
                    RecordId      = l.RecordId,
                    ActionDetails = l.ActionDetails,
                    OldInfo       = l.OldInfo,
                    CreationDate  = l.CreationDate,
                    Notes         = l.Notes
                })
                .ToListAsync();

            return Ok(logs);
        }

        // ── GET /api/log/admins ──────────────────────────────────────────────
        /// <summary>Returns the list of all admin users (id + name) for filter dropdowns.</summary>
        [HttpGet("admins")]
        public async Task<IActionResult> GetAdmins()
        {
            var admins = await _context.AdminUsers
                .AsNoTracking()
                .OrderBy(a => a.UserName)
                .Select(a => new { a.UserId, a.UserName })
                .ToListAsync();

            return Ok(admins);
        }
    }
}
