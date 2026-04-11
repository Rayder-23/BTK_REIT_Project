using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransfersController : ControllerBase
    {
        private readonly ReitContext _context;

        public TransfersController(ReitContext context)
        {
            _context = context;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteTransfer(int fundId, int fromShId, int toShId, decimal pct)
        {
            // 1. Start a Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. Find the Seller's current active stake
                var sellerStake = await _context.FundDetails
                    .FirstOrDefaultAsync(fd => fd.FundId == fundId && fd.ShId == fromShId && fd.EndDate == null);

                if (sellerStake == null || sellerStake.PctOwned < pct)
                    return BadRequest("Seller does not have enough stake.");

                // 3. "Close" the old record
                sellerStake.EndDate = DateOnly.FromDateTime(DateTime.Now);

                // 4. Create the new record for the Buyer
                var buyerNewRecord = new FundDetail
                {
                    FundId = fundId,
                    ShId = toShId,
                    PctOwned = pct, // The amount being bought
                    AcquiredDate = DateOnly.FromDateTime(DateTime.Now),
                    // Note: share_value logic would go here
                };

                // 5. Create the remaining stake record for the Seller (if any)
                if (sellerStake.PctOwned > pct)
                {
                    var sellerRemaining = new FundDetail
                    {
                        FundId = fundId,
                        ShId = fromShId,
                        PctOwned = sellerStake.PctOwned - pct,
                        AcquiredDate = DateOnly.FromDateTime(DateTime.Now)
                    };
                    _context.FundDetails.Add(sellerRemaining);
                }

                _context.FundDetails.Add(buyerNewRecord);
                
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Save everything at once!

                return Ok("Transfer completed successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"Transfer failed: {ex.Message}");
            }
        }
    }
}