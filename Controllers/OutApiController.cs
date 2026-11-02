using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class OutApiController : ControllerBase
    {
        private readonly IOutServiceApi _outapiService;
        private readonly IReportService _reportService;

        public OutApiController(IOutServiceApi outapiService, IReportService reportService)
        {
            _outapiService = outapiService;
            _reportService = reportService;
        }

        [HttpGet("dry-process-summary")]
        public async Task<IActionResult> GetDryProcessSummary(
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate)
        {
            var result = await _outapiService.GetDryProcessSummaryAsync(
                fromDate,
                toDate);

            return Ok(result);
        }

        // GET: api/OutApi/wash-process-report
        // Same data as /api/Report/export/short-csv but returned as JSON
        [HttpGet("wash-process-report")]
        public async Task<IActionResult> GetWashProcessReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                var rows = await _reportService.GetWashProcessReportAsync(request);

                return Ok(new
                {
                    success = true,
                    count = rows.Count,
                    data = rows
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error generating wash process report: {ex.Message}"
                });
            }
        }
    }
}
