using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using REIT_Project.DTOs;

namespace REIT_Project.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShareholdersController : ControllerBase
    {
        private readonly ReitContext _context;

        public ShareholdersController(ReitContext context)
        {
            _context = context;
        }

        // GET: api/shareholders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ShareholderDto>>> GetShareholders()
        {
            return await _context.Shareholders
                .Select(s => new ShareholderDto
                {
                    ShId = s.ShId,
                    ShType = s.ShType,
                    UserName = s.UserName,
                    Password = s.Password,
                    FullName = s.FullName,
                    Cnic = s.Cnic,
                    NtnNo = s.NtnNo,
                    PassportNo = s.PassportNo,
                    ContactNo = s.ContactNo,
                    ContactEmail = s.ContactEmail,
                    IsFiller = s.IsFiller,
                    IsOverseas = s.IsOverseas,
                    IsReit = s.IsReit,
                    CreationDate = s.CreationDate,
                    Status = s.Status
                })
                .ToListAsync();
        }

        // GET: api/shareholders/1
        [HttpGet("{id}")]
        public async Task<ActionResult<ShareholderDto>> GetShareholder(int id)
        {
            var shareholder = await _context.Shareholders
                .Where(s => s.ShId == id)
                .Select(s => new ShareholderDto
                {
                    ShId = s.ShId,
                    ShType = s.ShType,
                    UserName = s.UserName,
                    Password = s.Password,
                    FullName = s.FullName,
                    Cnic = s.Cnic,
                    NtnNo = s.NtnNo,
                    PassportNo = s.PassportNo,
                    ContactNo = s.ContactNo,
                    ContactEmail = s.ContactEmail,
                    IsFiller = s.IsFiller,
                    IsOverseas = s.IsOverseas,
                    IsReit = s.IsReit,
                    CreationDate = s.CreationDate,
                    Status = s.Status
                })
                .FirstOrDefaultAsync();

            if (shareholder == null)
            {
                return NotFound();
            }

            return shareholder;
        }
    }
}
