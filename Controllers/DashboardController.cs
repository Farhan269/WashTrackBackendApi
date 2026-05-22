using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    //[Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }


        [HttpGet("DasboardSummery")]
        public async Task<IActionResult> GetDashboardData(
            [FromQuery] DateOnly? fromDate,
            [FromQuery] DateOnly? toDate,
            [FromQuery] string? plant,
            [FromQuery] string? unit,
            [FromQuery] int? shift)
        {
            try
            {
                var result = await _dashboardService.GetDashboardDataAsync(
                    fromDate,
                    toDate,
                    plant,
                    unit,
                    shift,
                    User);

                return Ok(new
                {
                    success = true,
                    message = "Data retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    data = new List<DashboardDto>()
                });
            }
        }

        [HttpGet("DasboardDetails")]
        public async Task<IActionResult> GetDashboardDetailsAsync(
    [FromQuery] DateOnly? fromDate,
    [FromQuery] DateOnly? toDate,
    [FromQuery] string? plant,
    [FromQuery] string? unit,
    [FromQuery] int? shift,
    [FromQuery] List<int>? processStageIds,
    [FromQuery] string? search,
    [FromQuery] int? page,
    [FromQuery] int? pageSize)
        {
            try
            {
                // ✅ Convert nullable to actual values (service will set defaults if needed)
                var actualPage = page ?? 1;
                var actualPageSize = pageSize ?? 25;

                var result = await _dashboardService.GetDashboardDetailsAsync(
                    fromDate,
                    toDate,
                    plant,
                    unit,
                    shift,
                    processStageIds,
                    search,
                    actualPage,
                    actualPageSize,
                    User);

                return Ok(new
                {
                    success = true,
                    message = "Data retrieved successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error: {ex.Message}",
                    data = new List<DashboardDetailsDto>()
                });
            }
        }

        [HttpGet]
        [Route("PlantUnitList")]
        public async Task<IActionResult> PlantUnitListAsync()
        {
            try
            {
                var result = await _dashboardService.GetPlantUnitListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet("machines")]
        public async Task<IActionResult> GetMachineList(int? plantId = null, int? unitId = null)
        {
            var result = await _dashboardService.GetMachineListAsync(plantId, unitId);
            return Ok(result);
        }


    }
}
