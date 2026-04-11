using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertyController : ControllerBase
    {
        private static readonly HashSet<string> ValidPropTypes =
            new(StringComparer.OrdinalIgnoreCase) { "residential", "commercial", "mixed-use" };

        private static readonly HashSet<string> ValidStatuses =
            new(StringComparer.OrdinalIgnoreCase) { "active", "sold", "under-review" };

        // sh_id = 1 is the REIT entity that holds 100 % ownership at inception.
        private const int ReitShareholderId = 1;

        private readonly ReitContext _context;
        private readonly IAuditService _audit;

        public PropertyController(ReitContext context, IAuditService audit)
        {
            _context = context;
            _audit = audit;
        }

        // POST: api/Property/onboard
        [HttpPost("onboard")]
        public async Task<IActionResult> OnboardProperty([FromBody] OnboardPropertyDto dto)
        {
            // ── Model-annotation validation ───────────────────────────────────
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ── Business rule validation (fast-fail before opening transaction) ─
            if (!ValidPropTypes.Contains(dto.PropType))
                return BadRequest($"Invalid prop_type '{dto.PropType}'. Must be 'residential', 'commercial', or 'mixed-use'.");

            string resolvedStatus = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status;
            if (!ValidStatuses.Contains(resolvedStatus))
                return BadRequest($"Invalid status '{resolvedStatus}'. Must be 'active', 'sold', or 'under-review'.");

            DateOnly resolvedDateAdded = dto.DateAdded ?? DateOnly.FromDateTime(DateTime.Today);

            if (dto.DateRemoved.HasValue && dto.DateRemoved.Value <= resolvedDateAdded)
                return BadRequest($"date_removed ({dto.DateRemoved}) must be later than date_added ({resolvedDateAdded}).");

            if (string.IsNullOrWhiteSpace(dto.PropName))
                return BadRequest("prop_name must not be empty.");

            // ── Atomic transaction via execution strategy ─────────────────────
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // ── STEP 1: Property ──────────────────────────────────────
                    var property = new Property
                    {
                        PropType      = dto.PropType.ToLower(),
                        PropName      = dto.PropName.Trim(),
                        Address       = dto.Address,
                        City          = dto.City,
                        ProvinceState = dto.ProvinceState,
                        Country       = dto.Country,
                        DateAdded     = resolvedDateAdded,
                        DateRemoved   = dto.DateRemoved,
                        PurchasePrice = dto.PurchasePrice,
                        CurrentValue  = dto.CurrentValue,
                        Status        = resolvedStatus,
                        Notes         = dto.PropNotes
                    };

                    _context.Properties.Add(property);
                    // Flush to get prop_id from SQL Server identity.
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Property",
                        recordId: property.PropId,
                        action: $"INSERT: Property '{property.PropName}' onboarded. " +
                                $"Type={property.PropType}, PurchasePrice={property.PurchasePrice:F2}, Status={property.Status}.");

                    // ── STEP 2: TrustFund ─────────────────────────────────────
                    decimal fundValue = dto.FundTotalValue ?? dto.PurchasePrice;

                    var fund = new TrustFund
                    {
                        PropId         = property.PropId,
                        FundTotalValue = fundValue,
                        FundTitle1     = dto.FundTitle,                                     // fund_title (100 chars)
                        FundTitle      = dto.FundTitleLong ?? $"{property.PropName} Trust Fund", // FundTitle (500 chars)
                        StartDate      = dto.FundStartDate
                                         ?? new DateTime(resolvedDateAdded.Year,
                                                         resolvedDateAdded.Month,
                                                         resolvedDateAdded.Day, 0, 0, 0, DateTimeKind.Utc),
                        Notes          = dto.FundNotes
                        // CreationDate: omitted — DB default (getdate()) supplies it
                    };

                    _context.TrustFunds.Add(fund);
                    // Flush to get fund_id from SQL Server identity.
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "TrustFund",
                        recordId: fund.FundId,
                        action: $"INSERT: TrustFund created for PropertyID {property.PropId}. " +
                                $"FundTotalValue={fundValue:F2}, Title='{fund.FundTitle}'.");

                    // ── STEP 3: Seed initial REIT ownership (100 %) ───────────
                    var seedOwnership = new FundDetail
                    {
                        FundId       = fund.FundId,
                        ShId         = ReitShareholderId,
                        PctOwned     = 100.00m,
                        ShareValue   = fundValue,
                        AcquiredDate = resolvedDateAdded,
                        Notes        = "Initial REIT ownership seeded at onboarding."
                    };

                    _context.FundDetails.Add(seedOwnership);
                    // Flush to get fund_dt_id from SQL Server identity.
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "FundDetails",
                        recordId: seedOwnership.FundDtId,
                        action: $"INSERT: Initial ownership seeded for FundID {fund.FundId}. " +
                                $"ShareholderID={ReitShareholderId}, PctOwned=100.00, ShareValue={fundValue:F2}.");

                    // ── STEP 4: Persist all three audit log rows ──────────────
                    // (LogAction only stages entries; this SaveChangesAsync commits them)
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(PropertiesController.GetProperty),
                        controllerName: "Properties",
                        routeValues: new { id = property.PropId },
                        value: new
                        {
                            property.PropId,
                            property.PropName,
                            fund.FundId,
                            FundTotalValue  = fundValue,
                            seedOwnership.FundDtId
                        });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Property onboarding failed: {ex.Message}");
                }
            });
        }
    }
}
