using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Services;

namespace wsahRecieveDelivary.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All endpoints require authentication
    public class WorkOrderController : ControllerBase
    {
        private readonly IWorkOrderService _workOrderService;

        public WorkOrderController(IWorkOrderService workOrderService)
        {
            _workOrderService = workOrderService;
        }

        // ==========================================
        // GET ALL WORK ORDERS
        // ==========================================
        /// <summary>
        /// Get all work orders
        /// </summary>
        [HttpGet]
        //[AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var workOrders = await _workOrderService.GetAllAsync();
                return Ok(new
                {
                    success = true,
                    data = workOrders,
                    count = workOrders.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving work orders: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        /// <summary>
        /// Get work order by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var workOrder = await _workOrderService.GetByIdAsync(id);

                if (workOrder == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Work order with ID {id} not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = workOrder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving work order: {ex.Message}"
                });
            }
        }

        // ==========================================
        // GET BY WORK ORDER NO
        // ==========================================
        /// <summary>
        /// Get work order by Work Order Number
        /// </summary>
        [HttpGet("by-workorderno/{workOrderNo}")]
        public async Task<IActionResult> GetByWorkOrderNo(string workOrderNo)
        {
            try
            {
                var workOrder = await _workOrderService.GetByWorkOrderNoAsync(workOrderNo);

                if (workOrder == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Work order '{workOrderNo}' not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = workOrder
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error retrieving work order: {ex.Message}"
                });
            }
        }

        // ==========================================
        // CREATE WORK ORDER
        // ==========================================
        /// <summary>
        /// Create new work order (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] WorkOrderDto dto)
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
                var workOrder = await _workOrderService.CreateAsync(dto, userId);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = workOrder.Id },
                    new
                    {
                        success = true,
                        message = "Work order created successfully",
                        data = workOrder
                    });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error creating work order: {ex.Message}"
                });
            }
        }

        // ==========================================
        // UPDATE WORK ORDER
        // ==========================================
        /// <summary>
        /// Update existing work order (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] WorkOrderDto dto)
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
                var workOrder = await _workOrderService.UpdateAsync(id, dto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Work order updated successfully",
                    data = workOrder
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error updating work order: {ex.Message}"
                });
            }
        }

        // ==========================================
        // DELETE WORK ORDER
        // ==========================================
        /// <summary>
        /// Delete work order (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _workOrderService.DeleteAsync(id);

                if (!result)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = $"Work order with ID {id} not found"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Work order deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error deleting work order: {ex.Message}"
                });
            }
        }

        // ==========================================
        // BULK UPLOAD FROM EXCEL
        // ==========================================
        /// <summary>
        /// Bulk upload work orders from Excel file (Admin only)
        /// </summary>
        [HttpPost("bulk-upload")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> BulkUpload(IFormFile file)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var result = await _workOrderService.BulkUploadFromExcelAsync(file, userId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error processing bulk upload: {ex.Message}"
                });
            }
        }


        // ==========================================
        // GET PAGINATED WITH FAST SEARCH (ADD THIS)
        // ==========================================
        /// <summary>
        /// Get work orders with pagination, fast search, and advanced filters
        /// User can search ANY data (WorkOrderNo, Buyer, Style, Color, Quantity, etc.)
        /// </summary>
        /// <param name="request">Pagination parameters</param>
        [HttpGet("paginated")]
        [ProducesResponseType(typeof(PaginatedResponseDto<WorkOrderResponseDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPaginated([FromQuery] PaginationRequestDto request)
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

                var result = await _workOrderService.GetPaginatedAsync(request);

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
                    message = $"Error retrieving paginated work orders: {ex.Message}"
                });
            }
        }

        // ==========================================
        // DOWNLOAD EXCEL TEMPLATE
        // ==========================================
        /// <summary>
        /// Download Excel template for bulk upload
        /// </summary>
        [HttpGet("download-template")]
        [Authorize(Roles = "Admin")]
        public IActionResult DownloadTemplate()
        {
            try
            {
                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("WorkOrders");

                // Set headers
                string[] headers =
                {
                    "Factory", "Line", "Unit", "Buyer", "Buyer Department",
                    "Style Name", "FastReact No", "Color", "Work Order No",
                    "Wash Type", "Order Quantity", "Cut Qty", "TOD",
                    "Sewing Comp. Date", "1st RCV Date", "Wash Approval Date",
                    "Wash Target Date", "Total Wash Received", "Total Wash Delivery",
                    "Wash Balance", "From Received", "Marks"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }

                // Add sample data row
                worksheet.Cells[2, 1].Value = "TAL";
                worksheet.Cells[2, 2].Value = "Line E+G";
                worksheet.Cells[2, 3].Value = "Unit TWL";
                worksheet.Cells[2, 4].Value = "Zara";
                worksheet.Cells[2, 5].Value = "Zara TRF";
                worksheet.Cells[2, 6].Value = "FOLDED WAISTBAND (5252/214/400)";
                worksheet.Cells[2, 7].Value = "68051-D::Blue-400::301025";
                worksheet.Cells[2, 8].Value = "Blue-400";
                worksheet.Cells[2, 9].Value = "00062914";
                worksheet.Cells[2, 10].Value = "Acid Wash";
                worksheet.Cells[2, 11].Value = "15,000";
                worksheet.Cells[2, 12].Value = "15,449";
                worksheet.Cells[2, 13].Value = "30-Oct";
                worksheet.Cells[2, 14].Value = "21-Oct-25";
                worksheet.Cells[2, 15].Value = "17-Oct";
                worksheet.Cells[2, 16].Value = "06-Oct-25";
                worksheet.Cells[2, 17].Value = "12-Nov-25";
                worksheet.Cells[2, 18].Value = "15,442";
                worksheet.Cells[2, 19].Value = "916";
                worksheet.Cells[2, 20].Value = "14,526";
                worksheet.Cells[2, 21].Value = "";
                worksheet.Cells[2, 22].Value = "";

                // Auto-fit columns
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    "WorkOrder_Template.xlsx"
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error generating template: {ex.Message}"
                });
            }
        }
    }


}