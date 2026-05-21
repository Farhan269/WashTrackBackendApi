using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProcessStageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProcessStageController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ==========================================
        // GET ALL PROCESS STAGES
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var stages = await _context.ProcessStages
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .ToListAsync();

                return Ok(new { success = true, data = stages });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var stage = await _context.ProcessStages.FindAsync(id);
                if (stage == null)
                    return NotFound(new { success = false, message = "Process stage not found" });

                return Ok(new { success = true, data = stage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // CREATE NEW PROCESS STAGE (Admin Only)
        // ==========================================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] ProcessStage stage)
        {
            try
            {
                if (await _context.ProcessStages.AnyAsync(s => s.Name == stage.Name))
                    return BadRequest(new { success = false, message = "Process stage already exists" });

                stage.CreatedAt = DateTime.UtcNow;
                stage.IsActive = true;

                _context.ProcessStages.Add(stage);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = stage.Id },
                    new { success = true, message = "Process stage created", data = stage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // UPDATE PROCESS STAGE (Admin Only)
        // ==========================================
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] ProcessStage updatedStage)
        {
            try
            {
                var stage = await _context.ProcessStages.FindAsync(id);
                if (stage == null)
                    return NotFound(new { success = false, message = "Process stage not found" });

                stage.Name = updatedStage.Name;
                stage.Description = updatedStage.Description;
                stage.DisplayOrder = updatedStage.DisplayOrder;
                stage.IsActive = updatedStage.IsActive;
                stage.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Process stage updated", data = stage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // DELETE/DEACTIVATE PROCESS STAGE (Admin Only)
        // ==========================================
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var stage = await _context.ProcessStages.FindAsync(id);
                if (stage == null)
                    return NotFound(new { success = false, message = "Process stage not found" });

                // Soft delete
                stage.IsActive = false;
                stage.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Process stage deactivated" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}