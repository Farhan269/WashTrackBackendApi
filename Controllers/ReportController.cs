// D:\test c#\wsahRecieveDelivary\Controllers\ReportController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService reportService)
        {
            _reportService = reportService;
        }

        
        // ==========================================
        // GET TRANSACTION REPORT (MAIN ENDPOINT)
        // ==========================================
        /// <summary>
        /// Get transaction report with pagination, filters, and pre-calculated summary
        /// </summary>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(ReportResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
       
        public async Task<IActionResult> GetTransactionReport([FromQuery] ReportRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid request parameters",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _reportService.GetTransactionReportAsync(request);

                if (!result.Success)
                {
                    return StatusCode(500, result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error generating report: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET SUMMARY ONLY
        // ==========================================
        /// <summary>
        /// Get summary statistics only (for dashboard cards)
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ReportSummaryDto), 200)]
        public async Task<IActionResult> GetSummary([FromQuery] ReportRequestDto request)
        {
            try
            {
                var result = await _reportService.GetSummaryAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error getting summary: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET FILTER OPTIONS
        // ==========================================
        /// <summary>
        /// Get filter options for dropdowns
        /// </summary>
        [HttpGet("filter-options")]
        [ProducesResponseType(typeof(ReportFilterOptionsDto), 200)]
        public async Task<IActionResult> GetFilterOptions()
        {
            try
            {
                var result = await _reportService.GetFilterOptionsAsync();

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error getting filter options: {ex.Message}"
                });
            }
        }

        // ==========================================
        // EXPORT TO CSV
        // ==========================================
        /// <summary>
        /// Export report to CSV file with all filters applied
        /// </summary>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCsv([FromQuery] ReportRequestDto request)
        {
            try
            {
                var csvBytes = await _reportService.ExportToCsvAsync(request);

                if (csvBytes == null || csvBytes.Length == 0)
                {
                    return BadRequest(new { success = false, message = "No data to export" });
                }

                var fileName = $"Transaction_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

                return File(csvBytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error exporting report: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET USER TRANSACTION HISTORY
        // ==========================================
        /// <summary>
        /// Get transaction history for a specific user
        /// </summary>
        [HttpGet("user-transactions/{userId}")]
        [ProducesResponseType(typeof(List<UserTransactionHistoryDto>), 200)]
        public async Task<IActionResult> GetUserTransactionHistory(int userId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            try
            {
                var result = await _reportService.GetUserTransactionHistoryAsync(userId, startDate, endDate);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error getting user transaction history: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET USER WORK ORDER SUMMARY
        // ==========================================
        /// <summary>
        /// Get work order summary for a specific user
        /// </summary>
        [HttpGet("user-workorder-summary/{userId}")]
        [ProducesResponseType(typeof(List<UserWorkOrderSummaryDto>), 200)]
        public async Task<IActionResult> GetUserWorkOrderSummary(
            int userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? buyer,
            [FromQuery] string? factory,
            [FromQuery] string? unit,
            [FromQuery] int? processStageId)
        {
            try
            {
                var result = await _reportService.GetUserWorkOrderSummaryAsync(userId, startDate, endDate, buyer, factory, unit, processStageId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error getting user work order summary: {ex.Message}"
                });
            }
        }


        

    }
}