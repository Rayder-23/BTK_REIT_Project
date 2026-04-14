using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using REIT_Project.Models;
using BTK_REIT_Shared.DTOs;
using REIT_Project.Services;

namespace REIT_Project.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly ReitContext  _context;
    private readonly IAuditService _audit;

    public AuthController(ReitContext context, IAuditService audit)
    {
        _context = context;
        _audit   = audit;
    }

    /// <summary>
    /// Authenticate an admin user. Returns a session on success.
    /// POST /api/auth/login
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<UserSession>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.AdminUsers
            .Where(u => u.UserName == request.UserName && u.Password == request.Password)
            .FirstOrDefaultAsync();

        if (user is null)
            return Unauthorized(new { message = "Invalid username or password." });

        // Update last login timestamp
        user.LastLogin = DateTime.UtcNow;

        // Audit the login event
        _audit.LogAction(
            userId:    user.UserId,
            tableName: "AdminUsers",
            recordId:  user.UserId,
            action:    $"LOGIN: Admin '{user.UserName}' authenticated successfully at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC.");

        await _context.SaveChangesAsync();

        var session = new UserSession
        {
            UserId        = user.UserId,
            UserName      = user.UserName,
            SecurityLevel = user.SecurityLevel,
            Token         = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
        };

        return Ok(session);
    }
}
