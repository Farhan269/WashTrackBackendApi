// D:\test c#\wsahRecieveDelivary\Controllers\WashTransactionController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Formats.Asn1;
using System.Globalization;
using System.Security.Claims;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models.Enums;
using wsahRecieveDelivary.Services;
using CsvHelper; 
namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WashTransactionController : ControllerBase
    {
        private readonly IWashTransactionService _service;

        public WashTransactionController(IWashTransactionService service)
        {
            _service = service;
        }
        
        // ==========================================
        // CREATE RECEIVE
        // ==========================================
        /// <summary>
        /// Create a receive transaction
        /// </summary>
        [HttpPost("receive")]
        public async Task<IActionResult> CreateReceive([FromBody] CreateWashTransactionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _service.CreateReceiveAsync(dto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Receive transaction created successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // CREATE DELIVERY
        // ==========================================
        /// <summary>
        /// Create a delivery transaction
        /// </summary>
        [HttpPost("delivery")]
        public async Task<IActionResult> CreateDelivery([FromBody] CreateWashTransactionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _service.CreateDeliveryAsync(dto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Delivery transaction created successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET ALL TRANSACTIONS
        // ==========================================
        /// <summary>
        /// Get all transactions
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var transactions = await _service.GetAllAsync();
                return Ok(new
                {
                    success = true,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        /// <summary>
        /// Get transaction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var transaction = await _service.GetByIdAsync(id);
                if (transaction == null)
                {
                    return NotFound(new { success = false, message = $"Transaction with ID {id} not found" });
                }

                return Ok(new { success = true, data = transaction });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET BY WORK ORDER
        // ==========================================
        /// <summary>
        /// Get all transactions for a work order
        /// </summary>
        [HttpGet("workorder/{workOrderId}")]
        public async Task<IActionResult> GetByWorkOrder(int workOrderId)
        {
            try
            {
                var transactions = await _service.GetByWorkOrderAsync(workOrderId);
                return Ok(new
                {
                    success = true,
                    workOrderId = workOrderId,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET BY STAGE (int instead of enum)
        // ==========================================
        /// <summary>
        /// Get all transactions for a process stage
        /// </summary>
        [HttpGet("stage/{processStageId}")]
        public async Task<IActionResult> GetByStage(int processStageId)
        {
            try
            {
                var transactions = await _service.GetByStageAsync(processStageId);
                return Ok(new
                {
                    success = true,
                    processStageId = processStageId,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // FILTER TRANSACTIONS
        // ==========================================
        /// <summary>
        /// Filter transactions by multiple criteria
        /// </summary>
        [HttpPost("filter")]
        public async Task<IActionResult> Filter([FromBody] WashTransactionFilterDto filter)
        {
            try
            {
                var transactions = await _service.GetByFilterAsync(filter);
                return Ok(new
                {
                    success = true,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // UPDATE TRANSACTION
        // ==========================================
        /// <summary>
        /// Update transaction (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CreateWashTransactionDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _service.UpdateAsync(id, dto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Transaction updated successfully",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // DELETE TRANSACTION
        // ==========================================
        /// <summary>
        /// Delete transaction (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id);
                if (!result)
                {
                    return NotFound(new { success = false, message = $"Transaction with ID {id} not found" });
                }

                return Ok(new
                {
                    success = true,
                    message = "Transaction deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET BALANCES BY WORK ORDER
        // ==========================================
        /// <summary>
        /// Get stage-wise balances for a work order
        /// </summary>
        [HttpGet("balance/workorder/{workOrderId}")]
        public async Task<IActionResult> GetBalances(int workOrderId)
        {
            try
            {
                var balances = await _service.GetBalancesByWorkOrderAsync(workOrderId);
                return Ok(new
                {
                    success = true,
                    workOrderId = workOrderId,
                    data = balances
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET WASH STATUS
        // ==========================================
        /// <summary>
        /// Get complete wash status for a work order
        /// </summary>
        [HttpGet("status/workorder/{workOrderId}")]
        public async Task<IActionResult> GetWashStatus(int workOrderId)
        {
            try
            {
                var status = await _service.GetWashStatusAsync(workOrderId);
                if (status == null)
                {
                    return NotFound(new { success = false, message = $"WorkOrder with ID {workOrderId} not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET ALL WASH STATUSES
        // ==========================================
        /// <summary>
        /// Get wash status for all work orders
        /// </summary>
        [HttpGet("status/all")]
        public async Task<IActionResult> GetAllStatuses()
        {
            try
            {
                var statuses = await _service.GetAllWashStatusesAsync();
                return Ok(new
                {
                    success = true,
                    count = statuses.Count,
                    data = statuses
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET STAGE SUMMARY
        // ==========================================
        /// <summary>
        /// Get summary report for all stages
        /// </summary>
        [HttpGet("summary/stages")]
        public async Task<IActionResult> GetStageSummary()
        {
            try
            {
                var summary = await _service.GetStageSummaryAsync();
                return Ok(new
                {
                    success = true,
                    data = summary
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET RECEIVES BY STAGE (int instead of enum)
        // ==========================================
        /// <summary>
        /// Get all receive transactions for a stage
        /// </summary>
        [HttpGet("receives/stage/{processStageId}")]
        public async Task<IActionResult> GetReceivesByStage(int processStageId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var transactions = await _service.GetReceivesByStageAsync(processStageId, startDate, endDate);
                return Ok(new
                {
                    success = true,
                    processStageId = processStageId,
                    startDate = startDate,
                    endDate = endDate,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET DELIVERIES BY STAGE (int instead of enum)
        // ==========================================
        /// <summary>
        /// Get all delivery transactions for a stage
        /// </summary>
        [HttpGet("deliveries/stage/{processStageId}")]
        public async Task<IActionResult> GetDeliveriesByStage(int processStageId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var transactions = await _service.GetDeliveriesByStageAsync(processStageId, startDate, endDate);
                return Ok(new
                {
                    success = true,
                    processStageId = processStageId,
                    startDate = startDate,
                    endDate = endDate,
                    count = transactions.Count,
                    data = transactions
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // GET PAGINATED WITH FAST SEARCH
        // ==========================================
        /// <summary>
        /// Get transactions with pagination, fast search, and advanced filters
        /// User can search ANY data (WorkOrderNo, Buyer, Style, BatchNo, GatePassNo, etc.)
        /// </summary>
        [HttpGet("paginated")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(PaginatedResponseDto<WashTransactionResponseDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPaginated([FromQuery] TransactionPaginationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid pagination parameters",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var result = await _service.GetPaginatedAsync(request);

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
                    message = $"Error retrieving paginated transactions: {ex.Message}"
                });
            }
        }

        // ==========================================
        // ✅ NEW: GET USER TRANSACTIONS WITH SUMMARY
        // ==========================================
        /// <summary>
        /// Get all transactions created by current user with pagination, search, and summary
        /// Includes: Total counts, stage-wise summary, and paginated transaction list
        /// </summary>
        [HttpGet("user/summary")]
        [ProducesResponseType(typeof(UserTransactionSummaryDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetUserTransactionsSummary(
    [FromQuery] TransactionPaginationRequestDto request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0)
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var result = await _service.GetUserTransactionsSummaryAsync(userId, request);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        // ==========================================
        // EXPORT TO CSV
        // ==========================================
        /// <summary>
        /// Export transactions to CSV file with filters
        /// </summary>
        [HttpGet("export/csv")]
        public async Task<IActionResult> ExportToCSV(
            [FromQuery] string? searchTerm = null,
            [FromQuery] string? buyer = null,
            [FromQuery] string? factory = null,
            [FromQuery] string? unit = null,
            [FromQuery] int? processStageId = null,
            [FromQuery] int? transactionTypeId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                Console.WriteLine($"📥 Export request received");
                Console.WriteLine($"   SearchTerm: {searchTerm}");
                Console.WriteLine($"   Buyer: {buyer}");
                Console.WriteLine($"   Factory: {factory}");
                Console.WriteLine($"   Unit: {unit}");
                Console.WriteLine($"   ProcessStageId: {processStageId}");
                Console.WriteLine($"   TransactionTypeId: {transactionTypeId}");
                Console.WriteLine($"   StartDate: {startDate}");
                Console.WriteLine($"   EndDate: {endDate}");

                var csvBytes = await _service.ExportToCSVAsync(
                    searchTerm,
                    buyer,
                    factory,
                    unit,
                    processStageId,
                    transactionTypeId,
                    startDate,
                    endDate
                );

                if (csvBytes == null || csvBytes.Length == 0)
                {
                    Console.WriteLine("❌ Empty CSV generated");
                    return BadRequest(new { success = false, message = "No data to export" });
                }

                Console.WriteLine($"✅ CSV generated successfully - Size: {csvBytes.Length} bytes");

                var fileName = $"Transactions_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(
                    csvBytes,
                    "text/csv",
                    fileName
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Export error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message,
                    details = ex.StackTrace
                });
            }
        }

        [HttpPost("create/ShiftSchedule")]
        
       // [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateShiftScheduleDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid data",
                        errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                    });
                }

                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var schedule = await _service.CreateScheduleAsync(dto, userId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = schedule.Id },
                    new
                    {
                        success = true,
                        message = "Shift schedule created successfully",
                        data = schedule
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating schedule: {ex.Message}"
                });
            }
        }


        [HttpGet("active")]
        public async Task<IActionResult> GetActive([FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.UtcNow;
                var schedule = await _service.GetActiveScheduleAsync(targetDate);

                if (schedule == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "No active schedule found for this date"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = schedule
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving active schedule: {ex.Message}"
                });
            }
        }

        
    }
}