using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ApplicationDbContext _context;

        public AuthController(IAuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(registerDto);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(loginDto);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
            var isAdmin = roles.Contains("Admin");

            // ✅ CHANGED: Get ProcessStages instead of Categories
            List<string> stages;
            if (isAdmin)
            {
                // Admin gets all stages
                stages = await _context.ProcessStages
                    .Where(s => s.IsActive)
                    .Select(s => s.Name)
                    .ToListAsync();
            }
            else
            {
                // User gets stages from token
                stages = User.FindAll("ProcessStage").Select(c => c.Value).ToList();
            }

            return Ok(new
            {
                UserId = userId,
                Username = username,
                Roles = roles,
                IsAdmin = isAdmin,
                ProcessStages = stages,
                Message = isAdmin ? "Admin has access to all stages" : "User has limited stage access"
            });
        }
    }
}