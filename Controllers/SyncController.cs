using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SyncController : ControllerBase
    {
        private readonly IExternalApiSyncService _syncService;
        private readonly ILogger<SyncController> _logger;

        public SyncController(
            IExternalApiSyncService syncService,
            ILogger<SyncController> logger)
        {
            _syncService = syncService;
            _logger = logger;
        }

        /// <summary>
        /// Manually sync work orders from external API (Admin only)
        /// </summary>
        [HttpPost("sync-workorders")]
        [AllowAnonymous]
        //[Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SyncWorkOrders()
        {
            try
            {
                _logger.LogInformation("🔄 Manual sync initiated");

                var result = await _syncService.SyncWorkOrdersAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    data = new
                    {
                        totalFetched = result.TotalRecordsFetched,
                        created = result.CreatedCount,
                        updated = result.UpdatedCount,
                        // ✅ NEW: Added this line
                        upToDate = result.UpToDateCount,
                        failed = result.FailedCount,
                        startTime = result.SyncStartTime,
                        endTime = result.SyncEndTime,
                        duration = result.SyncEndTime.HasValue
                            ? (result.SyncEndTime.Value - result.SyncStartTime).TotalSeconds
                            : 0,
                        errors = result.Errors.Take(10).ToList()
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Sync failed");

                return StatusCode(500, new
                {
                    success = false,
                    message = $"Sync failed: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get last sync status
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStatus()
        {
            try
            {
                var status = await _syncService.GetLastSyncStatusAsync();

                return Ok(new
                {
                    success = true,
                    data = status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sync status");
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}