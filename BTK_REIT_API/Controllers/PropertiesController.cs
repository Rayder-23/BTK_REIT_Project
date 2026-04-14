using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/properties")]
    [Tags("Properties")]
    public class PropertiesController : ControllerBase
    {
        // sh_id = 1 is the REIT entity that holds 100% ownership at inception.
        private const int ReitShareholderId = 1;

        private readonly ReitContext _context;
        private readonly IAuditService _audit;
        private readonly IValidationService _validation;

        public PropertiesController(ReitContext context, IAuditService audit, IValidationService validation)
        {
            _context    = context;
            _audit      = audit;
            _validation = validation;
        }

        // ── GET /api/properties ──────────────────────────────────────────────
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyDto>>> GetProperties()
        {
            return await _context.Properties
                .Select(p => new PropertyDto
                {
                    PropId        = p.PropId,
                    PropType      = p.PropType,
                    PropName      = p.PropName,
                    Address       = p.Address,
                    City          = p.City,
                    ProvinceState = p.ProvinceState,
                    Country       = p.Country,
                    DateAdded     = p.DateAdded,
                    DateRemoved   = p.DateRemoved,
                    PurchasePrice = p.PurchasePrice,
                    CurrentValue  = p.CurrentValue,
                    Status        = p.Status,
                    Notes         = p.Notes
                })
                .ToListAsync();
        }

        // ── GET /api/properties/{id} ─────────────────────────────────────────
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            var property = await _context.Properties
                .Where(p => p.PropId == id)
                .Select(p => new PropertyDto
                {
                    PropId        = p.PropId,
                    PropType      = p.PropType,
                    PropName      = p.PropName,
                    Address       = p.Address,
                    City          = p.City,
                    ProvinceState = p.ProvinceState,
                    Country       = p.Country,
                    DateAdded     = p.DateAdded,
                    DateRemoved   = p.DateRemoved,
                    PurchasePrice = p.PurchasePrice,
                    CurrentValue  = p.CurrentValue,
                    Status        = p.Status,
                    Notes         = p.Notes
                })
                .FirstOrDefaultAsync();

            if (property == null)
                return NotFound();

            return property;
        }

        // ── GET /api/properties/{id}/full-details ────────────────────────────
        [HttpGet("{id:int}/full-details")]
        public async Task<ActionResult<PropertyDetailDto>> GetFullDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.TrustFund)
                .FirstOrDefaultAsync(p => p.PropId == id);

            if (property == null)
                return NotFound("Property not found.");

            if (property.TrustFund == null)
                return UnprocessableEntity(new
                {
                    error = $"Data Integrity Error: Property '{property.PropName}' exists but has no associated TrustFund record."
                });

            var detail = new PropertyDetailDto
            {
                PropName  = property.PropName,
                FundTitle = property.TrustFund.FundTitle ?? "Untitled Fund",
                TotalValue = property.TrustFund.FundTotalValue,
                CurrentOwners = await _context.FundDetails
                    .Where(fd => fd.FundId == property.TrustFund.FundId && fd.EndDate == null)
                    .Select(fd => new OwnerDto
                    {
                        FullName = fd.Sh.FullName,
                        PctOwned = fd.PctOwned
                    })
                    .ToListAsync()
            };

            return Ok(detail);
        }

        // ── PATCH /api/properties/{id} ───────────────────────────────────────
        /// <summary>
        /// Partial update for a property. Only non-null fields in the body are applied.
        /// Soft-delete is done by setting Status = "sold" or "under-review".
        /// Blocks physical deletion when a TrustFund with transactional history exists.
        /// </summary>
        [HttpPatch("{id:int}")]
        public async Task<IActionResult> PatchProperty(int id, [FromBody] PatchPropertyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var property = await _context.Properties
                .Include(p => p.TrustFund)
                .FirstOrDefaultAsync(p => p.PropId == id);

            if (property is null)
                return NotFound($"Property with prop_id={id} not found.");

            // ── Status validation ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                try
                {
                    var (statusValid, statusAllowed) = await _validation.IsValidAsync("status_prop", dto.Status);
                    if (!statusValid)
                        return BadRequest(new { error = "Invalid value", field = "status", allowed = statusAllowed });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ── PropType validation ───────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.PropType))
            {
                try
                {
                    var (typeValid, typeAllowed) = await _validation.IsValidAsync("prop_type", dto.PropType);
                    if (!typeValid)
                        return BadRequest(new { error = "Invalid value", field = "prop_type", allowed = typeAllowed });
                }
                catch (InvalidOperationException ex)
                {
                    return StatusCode(500, ex.Message);
                }
            }

            // ── Date coherence ────────────────────────────────────────────────
            DateOnly effectiveDateAdded  = dto.DateAdded  ?? property.DateAdded;
            DateOnly? effectiveDateRemoved = dto.DateRemoved ?? property.DateRemoved;
            if (effectiveDateRemoved.HasValue && effectiveDateRemoved.Value <= effectiveDateAdded)
                return BadRequest($"date_removed ({effectiveDateRemoved}) must be later than date_added ({effectiveDateAdded}).");

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Capture old state for audit
                    string oldSnapshot =
                        $"PropName={property.PropName}, PropType={property.PropType}, " +
                        $"Status={property.Status}, PurchasePrice={property.PurchasePrice:F2}, " +
                        $"CurrentValue={property.CurrentValue?.ToString("F2") ?? "null"}";

                    // Apply only provided fields
                    if (!string.IsNullOrWhiteSpace(dto.PropName))      property.PropName      = dto.PropName.Trim();
                    if (!string.IsNullOrWhiteSpace(dto.PropType))      property.PropType      = dto.PropType.ToLower();
                    if (!string.IsNullOrWhiteSpace(dto.Address))       property.Address       = dto.Address;
                    if (!string.IsNullOrWhiteSpace(dto.City))          property.City          = dto.City;
                    if (dto.ProvinceState is not null)                 property.ProvinceState = string.IsNullOrWhiteSpace(dto.ProvinceState) ? null : dto.ProvinceState;
                    if (!string.IsNullOrWhiteSpace(dto.Country))       property.Country       = dto.Country;
                    if (dto.DateAdded.HasValue)                        property.DateAdded     = dto.DateAdded.Value;
                    if (dto.DateRemoved.HasValue)                      property.DateRemoved   = dto.DateRemoved.Value;
                    if (dto.PurchasePrice.HasValue)                    property.PurchasePrice = dto.PurchasePrice.Value;
                    if (dto.CurrentValue.HasValue)                     property.CurrentValue  = dto.CurrentValue.Value;
                    if (!string.IsNullOrWhiteSpace(dto.Status))        property.Status        = dto.Status.ToLower();
                    if (dto.Notes is not null)                         property.Notes         = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes;

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "Property",
                        recordId: property.PropId,
                        action: $"PATCH: Property '{property.PropName}' (prop_id={id}) updated.",
                        oldInfo: oldSnapshot);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new PropertyDto
                    {
                        PropId        = property.PropId,
                        PropType      = property.PropType,
                        PropName      = property.PropName,
                        Address       = property.Address,
                        City          = property.City,
                        ProvinceState = property.ProvinceState,
                        Country       = property.Country,
                        DateAdded     = property.DateAdded,
                        DateRemoved   = property.DateRemoved,
                        PurchasePrice = property.PurchasePrice,
                        CurrentValue  = property.CurrentValue,
                        Status        = property.Status,
                        Notes         = property.Notes
                    });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, $"Property update failed: {ex.Message}");
                }
            });
        }

        // ── POST /api/properties/onboard ─────────────────────────────────────
        [HttpPost("onboard")]
        public async Task<IActionResult> OnboardProperty([FromBody] OnboardPropertyDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string resolvedStatus = string.IsNullOrWhiteSpace(dto.Status) ? "active" : dto.Status;

            try
            {
                var (propTypeValid, propTypeAllowed) = await _validation.IsValidAsync("prop_type", dto.PropType);
                if (!propTypeValid)
                    return BadRequest(new { error = "Invalid value", field = "prop_type", allowed = propTypeAllowed });

                var (statusValid, statusAllowed) = await _validation.IsValidAsync("status_prop", resolvedStatus);
                if (!statusValid)
                    return BadRequest(new { error = "Invalid value", field = "status", allowed = statusAllowed });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, ex.Message);
            }

            DateOnly resolvedDateAdded = dto.DateAdded ?? DateOnly.FromDateTime(DateTime.Today);

            if (dto.DateRemoved.HasValue && dto.DateRemoved.Value <= resolvedDateAdded)
                return BadRequest($"date_removed ({dto.DateRemoved}) must be later than date_added ({resolvedDateAdded}).");

            if (string.IsNullOrWhiteSpace(dto.PropName))
                return BadRequest("prop_name must not be empty.");

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
                        FundTitle1     = dto.FundTitle,
                        FundTitle      = dto.FundTitleLong ?? $"{property.PropName} Trust Fund",
                        StartDate      = dto.FundStartDate
                                         ?? new DateTime(resolvedDateAdded.Year,
                                                         resolvedDateAdded.Month,
                                                         resolvedDateAdded.Day, 0, 0, 0, DateTimeKind.Utc),
                        Notes          = dto.FundNotes
                    };

                    _context.TrustFunds.Add(fund);
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "TrustFund",
                        recordId: fund.FundId,
                        action: $"INSERT: TrustFund created for PropertyID {property.PropId}. " +
                                $"FundTotalValue={fundValue:F2}, Title='{fund.FundTitle}'.");

                    // ── STEP 3: Seed initial REIT ownership (100%) ────────────
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
                    await _context.SaveChangesAsync();

                    _audit.LogAction(
                        userId: dto.UserId,
                        tableName: "FundDetails",
                        recordId: seedOwnership.FundDtId,
                        action: $"INSERT: Initial ownership seeded for FundID {fund.FundId}. " +
                                $"ShareholderID={ReitShareholderId}, PctOwned=100.00, ShareValue={fundValue:F2}.");

                    // ── STEP 4: Persist all staged audit log rows ─────────────
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return CreatedAtAction(
                        actionName: nameof(GetProperty),
                        routeValues: new { id = property.PropId },
                        value: new
                        {
                            property.PropId,
                            property.PropName,
                            fund.FundId,
                            FundTotalValue = fundValue,
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
