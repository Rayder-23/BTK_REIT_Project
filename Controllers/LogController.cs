using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogController : ControllerBase
    {
        private readonly ReitContext _context;

        public LogController(ReitContext context)
        {
            _context = context;
        }

        // ── GET /api/Log/recent ──────────────────────────────────────────────
        /// <summary>
        /// Returns audit log entries ordered by creation_date descending.
        /// Optional filters: tableName, userId, limit (default 50).
        /// </summary>
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent(
            [FromQuery] string? tableName,
            [FromQuery] int?    userId,
            [FromQuery] int     limit = 50)
        {
            if (limit <= 0 || limit > 1000)
                return BadRequest("limit must be between 1 and 1000.");

            var query = _context.Logs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(tableName))
                query = query.Where(l => l.TableName == tableName);

            if (userId.HasValue)
                query = query.Where(l => l.UserId == userId.Value);

            var logs = await query
                .OrderByDescending(l => l.CreationDate)
                .Take(limit)
                .Select(l => new LogDto
                {
                    LogId         = l.LogId,
                    UserId        = l.UserId,
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
    }
}
