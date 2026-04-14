using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/funddetails")]
    [Tags("Fund Details")]
    public class FundDetailsController : ControllerBase
    {
        private readonly ReitContext _context;

        public FundDetailsController(ReitContext context)
        {
            _context = context;
        }

        // ── GET /api/funddetails/fund/{fundId} ────────────────────────────────
        /// <summary>
        /// Returns all active ownership records for a given fund (EndDate IS NULL).
        /// Includes the shareholder's full name for the cap table display.
        /// </summary>
        [HttpGet("fund/{fundId:int}")]
        public async Task<IActionResult> GetByFund(int fundId)
        {
            var fundExists = await _context.TrustFunds
                .AsNoTracking()
                .AnyAsync(f => f.FundId == fundId);

            if (!fundExists)
                return NotFound($"TrustFund with fund_id={fundId} not found.");

            var details = await _context.FundDetails
                .AsNoTracking()
                .Where(fd => fd.FundId == fundId && fd.EndDate == null)
                .OrderByDescending(fd => fd.PctOwned)
                .Select(fd => new FundOwnerDto
                {
                    ShId         = fd.ShId,
                    FullName     = fd.Sh.FullName,
                    PctOwned     = fd.PctOwned,
                    ShareValue   = fd.ShareValue,
                    AcquiredDate = fd.AcquiredDate
                })
                .ToListAsync();

            return Ok(details);
        }
    }
}
