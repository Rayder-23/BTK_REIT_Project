using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrustFundsController : ControllerBase
    {
        private readonly ReitContext _context;

        public TrustFundsController(ReitContext context)
        {
            _context = context;
        }

        // GET: api/trustfunds
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TrustFundDto>>> GetTrustFunds()
        {
            return await _context.TrustFunds
                .Select(f => new TrustFundDto
                {
                    FundId = f.FundId,
                    PropId = f.PropId,
                    FundTotalValue = f.FundTotalValue,
                    Notes = f.Notes,
                    StartDate = f.StartDate,
                    FundTitle = f.FundTitle,
                    FundTitle1 = f.FundTitle1,
                    CreationDate = f.CreationDate
                })
                .ToListAsync();
        }

        // GET: api/trustfunds/1
        [HttpGet("{id}")]
        public async Task<ActionResult<TrustFundDto>> GetTrustFund(int id)
        {
            var fund = await _context.TrustFunds
                .Where(f => f.FundId == id)
                .Select(f => new TrustFundDto
                {
                    FundId = f.FundId,
                    PropId = f.PropId,
                    FundTotalValue = f.FundTotalValue,
                    Notes = f.Notes,
                    StartDate = f.StartDate,
                    FundTitle = f.FundTitle,
                    FundTitle1 = f.FundTitle1,
                    CreationDate = f.CreationDate
                })
                .FirstOrDefaultAsync();

            if (fund == null)
            {
                return NotFound();
            }

            return fund;
        }
    }
}
