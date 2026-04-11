using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PropertiesController : ControllerBase
    {
        private readonly ReitContext _context;

        public PropertiesController(ReitContext context)
        {
            _context = context;
        }

        // GET: api/properties
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PropertyDto>>> GetProperties()
        {
            return await _context.Properties
                .Select(p => new PropertyDto
                {
                    PropId = p.PropId,
                    PropType = p.PropType,
                    PropName = p.PropName,
                    Address = p.Address,
                    City = p.City,
                    ProvinceState = p.ProvinceState,
                    Country = p.Country,
                    DateAdded = p.DateAdded,
                    DateRemoved = p.DateRemoved,
                    PurchasePrice = p.PurchasePrice,
                    CurrentValue = p.CurrentValue,
                    Status = p.Status,
                    Notes = p.Notes
                })
                .ToListAsync();
        }

        // GET: api/properties/1
        [HttpGet("{id}")]
        public async Task<ActionResult<PropertyDto>> GetProperty(int id)
        {
            var property = await _context.Properties
                .Where(p => p.PropId == id)
                .Select(p => new PropertyDto
                {
                    PropId = p.PropId,
                    PropType = p.PropType,
                    PropName = p.PropName,
                    Address = p.Address,
                    City = p.City,
                    ProvinceState = p.ProvinceState,
                    Country = p.Country,
                    DateAdded = p.DateAdded,
                    DateRemoved = p.DateRemoved,
                    PurchasePrice = p.PurchasePrice,
                    CurrentValue = p.CurrentValue,
                    Status = p.Status,
                    Notes = p.Notes
                })
                .FirstOrDefaultAsync();

            if (property == null)
            {
                return NotFound();
            }

            return property;
        }


        [HttpGet("{id}/full-details")]
        public async Task<ActionResult<PropertyDetailDto>> GetFullDetails(int id)
        {
            var property = await _context.Properties
                .Include(p => p.TrustFund)
                .FirstOrDefaultAsync(p => p.PropId == id);

            if (property == null) return NotFound("Property not found.");

            // Enforce Business Logic: Every Property MUST have a TrustFund
            if (property.TrustFund == null)
            {
                return UnprocessableEntity(new { 
                    error = $"Data Integrity Error: Property '{property.PropName}' exists but has no associated TrustFund record." 
                });
            }

            // Now we safely map to the DTO
            var detail = new PropertyDetailDto
            {
                PropName = property.PropName,
                FundTitle = property.TrustFund.FundTitle ?? "Untitled Fund", // Fallback for your empty column
                TotalValue = property.TrustFund.FundTotalValue,
                CurrentOwners = await _context.FundDetails
                    .Where(fd => fd.FundId == property.TrustFund.FundId && fd.EndDate == null)
                    .Select(fd => new OwnerDto
                    {
                        FullName = fd.Sh.FullName,
                        PctOwned = fd.PctOwned
                    }).ToListAsync()
            };

            return Ok(detail);
        }
    }
}
