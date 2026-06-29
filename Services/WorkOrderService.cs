//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore;
//using OfficeOpenXml;
//using System.Globalization;
//using wsahRecieveDelivary.Data;
//using wsahRecieveDelivary.DTOs;
//using wsahRecieveDelivary.Extensions;
//using wsahRecieveDelivary.Models;

//namespace wsahRecieveDelivary.Services
//{
//    public class WorkOrderService : IWorkOrderService
//    {
//        private readonly ApplicationDbContext _context;

//        public WorkOrderService(ApplicationDbContext context)
//        {
//            _context = context;
//            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
//        }

//        // ==========================================
//        // CREATE WORK ORDER
//        // ==========================================
//        public async Task<WorkOrderResponseDto> CreateAsync(WorkOrderDto dto, int userId)
//        {
//            if (await _context.WorkOrders.AnyAsync(w => w.WorkOrderNo == dto.WorkOrderNo))
//            {
//                throw new InvalidOperationException($"Work Order No '{dto.WorkOrderNo}' already exists");
//            }

//            var workOrder = new WorkOrder
//            {
//                Factory = dto.Factory,
//                Line = dto.Line,
//                Unit = dto.Unit,
//                Buyer = dto.Buyer,
//                BuyerDepartment = dto.BuyerDepartment,
//                StyleName = dto.StyleName,
//                FastReactNo = dto.FastReactNo,
//                Color = dto.Color,
//                WorkOrderNo = dto.WorkOrderNo,
//                WashType = dto.WashType,
//                OrderQuantity = dto.OrderQuantity,
//                CutQty = dto.CutQty,
//                TOD = dto.TOD,
//                SewingCompDate = dto.SewingCompDate,
//                FirstRCVDate = dto.FirstRCVDate,
//                WashApprovalDate = dto.WashApprovalDate,
//                WashTargetDate = dto.WashTargetDate,
//                TotalWashReceived = dto.TotalWashReceived,
//                TotalWashDelivery = dto.TotalWashDelivery,
//                WashBalance = dto.WashBalance,
//                FromReceived = dto.FromReceived,
//                Marks = dto.Marks,
//                CreatedBy = userId,
//                CreatedAt = DateTime.UtcNow
//            };

//            _context.WorkOrders.Add(workOrder);
//            await _context.SaveChangesAsync();

//            return await GetByIdAsync(workOrder.Id)
//                ?? throw new Exception("Failed to retrieve created work order");
//        }

//        // ==========================================
//        // UPDATE WORK ORDER
//        // ==========================================
//        public async Task<WorkOrderResponseDto> UpdateAsync(int id, WorkOrderDto dto, int userId)
//        {
//            var workOrder = await _context.WorkOrders.FindAsync(id);
//            if (workOrder == null)
//            {
//                throw new KeyNotFoundException($"Work Order with ID {id} not found");
//            }

//            if (workOrder.WorkOrderNo != dto.WorkOrderNo &&
//                await _context.WorkOrders.AnyAsync(w => w.WorkOrderNo == dto.WorkOrderNo))
//            {
//                throw new InvalidOperationException($"Work Order No '{dto.WorkOrderNo}' already exists");
//            }

//            workOrder.Factory = dto.Factory;
//            workOrder.Line = dto.Line;
//            workOrder.Unit = dto.Unit;
//            workOrder.Buyer = dto.Buyer;
//            workOrder.BuyerDepartment = dto.BuyerDepartment;
//            workOrder.StyleName = dto.StyleName;
//            workOrder.FastReactNo = dto.FastReactNo;
//            workOrder.Color = dto.Color;
//            workOrder.WorkOrderNo = dto.WorkOrderNo;
//            workOrder.WashType = dto.WashType;
//            workOrder.OrderQuantity = dto.OrderQuantity;
//            workOrder.CutQty = dto.CutQty;
//            workOrder.TOD = dto.TOD;
//            workOrder.SewingCompDate = dto.SewingCompDate;
//            workOrder.FirstRCVDate = dto.FirstRCVDate;
//            workOrder.WashApprovalDate = dto.WashApprovalDate;
//            workOrder.WashTargetDate = dto.WashTargetDate;
//            workOrder.TotalWashReceived = dto.TotalWashReceived;
//            workOrder.TotalWashDelivery = dto.TotalWashDelivery;
//            workOrder.WashBalance = dto.WashBalance;
//            workOrder.FromReceived = dto.FromReceived;
//            workOrder.Marks = dto.Marks;
//            workOrder.UpdatedBy = userId;
//            workOrder.UpdatedAt = DateTime.UtcNow;

//            await _context.SaveChangesAsync();

//            return await GetByIdAsync(workOrder.Id)
//                ?? throw new Exception("Failed to retrieve updated work order");
//        }

//        // ==========================================
//        // DELETE WORK ORDER
//        // ==========================================
//        public async Task<bool> DeleteAsync(int id)
//        {
//            var workOrder = await _context.WorkOrders.FindAsync(id);
//            if (workOrder == null)
//                return false;

//            _context.WorkOrders.Remove(workOrder);
//            await _context.SaveChangesAsync();
//            return true;
//        }

//        // ==========================================
//        // GET BY ID (WITH STAGE BALANCES - SEPARATE QUERY)
//        // ==========================================
//        public async Task<WorkOrderResponseDto?> GetByIdAsync(int id)
//        {
//            // Get work order with user info
//            var workOrder = await _context.WorkOrders
//                .Include(w => w.CreatedByUser)
//                .Include(w => w.UpdatedByUser)
//                .FirstOrDefaultAsync(w => w.Id == id);

//            if (workOrder == null)
//                return null;

//            // Get stage balances separately (since no navigation property in WorkOrder)
//            var stageBalances = await _context.ProcessStageBalances
//                .Include(psb => psb.ProcessStage)
//                .Where(psb => psb.WorkOrderId == id)
//                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
//                .ToListAsync();

//            return MapToResponseDto(workOrder, stageBalances);
//        }

//        // ==========================================
//        // GET BY WORK ORDER NO (WITH STAGE BALANCES)
//        // ==========================================
//        public async Task<WorkOrderResponseDto?> GetByWorkOrderNoAsync(string workOrderNo)
//        {
//            var workOrder = await _context.WorkOrders
//                .Include(w => w.CreatedByUser)
//                .Include(w => w.UpdatedByUser)
//                .FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);

//            if (workOrder == null)
//                return null;

//            // Get stage balances separately
//            var stageBalances = await _context.ProcessStageBalances
//                .Include(psb => psb.ProcessStage)
//                .Where(psb => psb.WorkOrderId == workOrder.Id)
//                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
//                .ToListAsync();

//            return MapToResponseDto(workOrder, stageBalances);
//        }

//        // ==========================================
//        // GET ALL (WITH STAGE BALANCES)
//        // ==========================================
//        public async Task<List<WorkOrderResponseDto>> GetAllAsync()
//        {
//            // Get all work orders
//            var workOrders = await _context.WorkOrders
//                .Include(w => w.CreatedByUser)
//                .Include(w => w.UpdatedByUser)
//                .OrderByDescending(w => w.CreatedAt)
//                .ToListAsync();

//            if (!workOrders.Any())
//                return new List<WorkOrderResponseDto>();

//            // Get all work order IDs
//            var workOrderIds = workOrders.Select(w => w.Id).ToList();

//            // Get all stage balances for these work orders in one query
//            var allStageBalances = await _context.ProcessStageBalances
//                .Include(psb => psb.ProcessStage)
//                .Where(psb => workOrderIds.Contains(psb.WorkOrderId))
//                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
//                .ToListAsync();

//            // Group balances by WorkOrderId for easy lookup
//            var balancesByWorkOrder = allStageBalances
//                .GroupBy(psb => psb.WorkOrderId)
//                .ToDictionary(g => g.Key, g => g.ToList());

//            // Map to DTOs
//            var result = new List<WorkOrderResponseDto>();
//            foreach (var wo in workOrders)
//            {
//                var stageBalances = balancesByWorkOrder.ContainsKey(wo.Id)
//                    ? balancesByWorkOrder[wo.Id]
//                    : new List<ProcessStageBalance>();

//                result.Add(MapToResponseDto(wo, stageBalances));
//            }

//            return result;
//        }

//        // ==========================================
//        // GET PAGINATED (WITH STAGE BALANCES)
//        // ==========================================
//        public async Task<PaginatedResponseDto<WorkOrderResponseDto>> GetPaginatedAsync(
//            PaginationRequestDto request)
//        {
//            try
//            {
//                // Build base query
//                var query = _context.WorkOrders
//                    .AsNoTracking()
//                    .Include(w => w.CreatedByUser)
//                    .Include(w => w.UpdatedByUser)
//                    .AsQueryable();

//                // Apply search
//                query = query.Search(request.SearchTerm);

//                // Apply filters
//                query = query.ApplyFilters(
//                    request.Factory,
//                    request.Buyer,
//                    request.WashType,
//                    request.Line,
//                    request.Unit,
//                    request.FromDate,
//                    request.ToDate
//                );

//                // Apply sorting
//                query = query.ApplySort(request.SortBy, request.SortOrder);

//                // Get total count
//                var totalCount = await query.CountAsync();
//                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

//                // Apply pagination
//                var skip = (request.Page - 1) * request.PageSize;
//                var workOrders = await query
//                    .Skip(skip)
//                    .Take(request.PageSize)
//                    .ToListAsync();

//                if (!workOrders.Any())
//                {
//                    return new PaginatedResponseDto<WorkOrderResponseDto>
//                    {
//                        Success = true,
//                        Message = "No records found",
//                        Data = new List<WorkOrderResponseDto>(),
//                        Pagination = new PaginationMetadata
//                        {
//                            CurrentPage = request.Page,
//                            PageSize = request.PageSize,
//                            TotalRecords = 0,
//                            TotalPages = 0,
//                            HasPrevious = false,
//                            HasNext = false
//                        }
//                    };
//                }

//                // Get work order IDs for this page
//                var workOrderIds = workOrders.Select(w => w.Id).ToList();

//                // Get stage balances for these work orders
//                var allStageBalances = await _context.ProcessStageBalances
//                    .AsNoTracking()
//                    .Include(psb => psb.ProcessStage)
//                    .Where(psb => workOrderIds.Contains(psb.WorkOrderId))
//                    .OrderBy(psb => psb.ProcessStage.DisplayOrder)
//                    .ToListAsync();

//                // Group balances by WorkOrderId
//                var balancesByWorkOrder = allStageBalances
//                    .GroupBy(psb => psb.WorkOrderId)
//                    .ToDictionary(g => g.Key, g => g.ToList());

//                // Map to DTOs
//                var data = new List<WorkOrderResponseDto>();
//                foreach (var wo in workOrders)
//                {
//                    var stageBalances = balancesByWorkOrder.ContainsKey(wo.Id)
//                        ? balancesByWorkOrder[wo.Id]
//                        : new List<ProcessStageBalance>();

//                    data.Add(MapToResponseDto(wo, stageBalances));
//                }

//                return new PaginatedResponseDto<WorkOrderResponseDto>
//                {
//                    Success = true,
//                    Message = null,
//                    Data = data,
//                    Pagination = new PaginationMetadata
//                    {
//                        CurrentPage = request.Page,
//                        PageSize = request.PageSize,
//                        TotalRecords = totalCount,
//                        TotalPages = totalPages,
//                        HasPrevious = request.Page > 1,
//                        HasNext = request.Page < totalPages
//                    }
//                };
//            }
//            catch (Exception ex)
//            {
//                return new PaginatedResponseDto<WorkOrderResponseDto>
//                {
//                    Success = false,
//                    Message = $"Error: {ex.Message}",
//                    Data = new List<WorkOrderResponseDto>(),
//                    Pagination = new PaginationMetadata()
//                };
//            }
//        }

//        // ==========================================
//        // BULK UPLOAD FROM EXCEL
//        // ==========================================
//        public async Task<WorkOrderBulkUploadResponseDto> BulkUploadFromExcelAsync(IFormFile file, int userId)
//        {
//            var response = new WorkOrderBulkUploadResponseDto { Success = false };

//            try
//            {
//                if (file == null || file.Length == 0)
//                {
//                    response.Message = "No file uploaded";
//                    return response;
//                }

//                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
//                {
//                    response.Message = "Invalid file format. Please upload Excel file (.xlsx or .xls)";
//                    return response;
//                }

//                using var stream = new MemoryStream();
//                await file.CopyToAsync(stream);

//                using var package = new ExcelPackage(stream);
//                var worksheet = package.Workbook.Worksheets[0];
//                var rowCount = worksheet.Dimension?.Rows ?? 0;

//                if (rowCount < 2)
//                {
//                    response.Message = "Excel file has no data rows";
//                    return response;
//                }

//                // Count actual data rows
//                int actualDataRows = 0;
//                for (int row = 2; row <= rowCount; row++)
//                {
//                    var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
//                    if (!string.IsNullOrEmpty(workOrderNo))
//                        actualDataRows++;
//                }

//                response.TotalRecords = actualDataRows;

//                for (int row = 2; row <= rowCount; row++)
//                {
//                    try
//                    {
//                        var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

//                        if (string.IsNullOrEmpty(workOrderNo))
//                            continue;

//                        var existingWorkOrder = await _context.WorkOrders
//                            .FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);

//                        var workOrder = existingWorkOrder ?? new WorkOrder();

//                        // Map Excel columns
//                        workOrder.Factory = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
//                        workOrder.Line = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
//                        workOrder.Unit = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
//                        workOrder.Buyer = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "";
//                        workOrder.BuyerDepartment = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? "";
//                        workOrder.StyleName = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? "";
//                        workOrder.FastReactNo = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "";
//                        workOrder.Color = worksheet.Cells[row, 8].Value?.ToString()?.Trim() ?? "";
//                        workOrder.WorkOrderNo = workOrderNo;
//                        workOrder.WashType = worksheet.Cells[row, 10].Value?.ToString()?.Trim() ?? "";

//                        // Parse quantities
//                        workOrder.OrderQuantity = ParseInt(worksheet.Cells[row, 11].Value?.ToString());
//                        workOrder.CutQty = ParseInt(worksheet.Cells[row, 12].Value?.ToString());

//                        // Parse dates
//                        workOrder.TOD = ParseDate(worksheet.Cells[row, 13].Value);
//                        workOrder.SewingCompDate = ParseDate(worksheet.Cells[row, 14].Value);
//                        workOrder.FirstRCVDate = ParseDate(worksheet.Cells[row, 15].Value);
//                        workOrder.WashApprovalDate = ParseDate(worksheet.Cells[row, 16].Value);
//                        workOrder.WashTargetDate = ParseDate(worksheet.Cells[row, 17].Value);

//                        // Parse wash quantities
//                        workOrder.TotalWashReceived = ParseInt(worksheet.Cells[row, 18].Value?.ToString());
//                        workOrder.TotalWashDelivery = ParseInt(worksheet.Cells[row, 19].Value?.ToString());

//                        var washBalanceStr = worksheet.Cells[row, 20].Value?.ToString()?.Trim();
//                        workOrder.WashBalance = ParseInt(washBalanceStr);

//                        if (workOrder.WashBalance == 0 &&
//                            (workOrder.TotalWashReceived > 0 || workOrder.TotalWashDelivery > 0))
//                        {
//                            workOrder.WashBalance = workOrder.TotalWashReceived - workOrder.TotalWashDelivery;
//                        }

//                        var col21Value = worksheet.Cells[row, 21].Value?.ToString()?.Trim();
//                        if (!string.IsNullOrEmpty(col21Value) &&
//                            int.TryParse(col21Value.Replace(",", ""), out _))
//                        {
//                            workOrder.FromReceived = ParseInt(col21Value);
//                            workOrder.Marks = worksheet.Cells[row, 22].Value?.ToString()?.Trim();
//                        }
//                        else
//                        {
//                            workOrder.FromReceived = 0;
//                            workOrder.Marks = col21Value;
//                        }

//                        if (existingWorkOrder != null)
//                        {
//                            workOrder.UpdatedBy = userId;
//                            workOrder.UpdatedAt = DateTime.UtcNow;
//                            response.UpdatedCount++;
//                        }
//                        else
//                        {
//                            workOrder.CreatedBy = userId;
//                            workOrder.CreatedAt = DateTime.UtcNow;
//                            _context.WorkOrders.Add(workOrder);
//                            response.SuccessCount++;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
//                        if (!string.IsNullOrEmpty(workOrderNo))
//                        {
//                            response.FailedCount++;
//                            response.Errors.Add($"Row {row} (WO: {workOrderNo}): {ex.Message}");
//                        }
//                    }
//                }

//                await _context.SaveChangesAsync();

//                response.Success = true;
//                response.Message = $"Bulk upload completed. Success: {response.SuccessCount}, Updated: {response.UpdatedCount}, Failed: {response.FailedCount}";
//            }
//            catch (Exception ex)
//            {
//                response.Success = false;
//                response.Message = $"Error processing file: {ex.Message}";
//            }

//            return response;
//        }

//        // ==========================================
//        // PRIVATE: MAP TO RESPONSE DTO
//        // ==========================================
//        private WorkOrderResponseDto MapToResponseDto(WorkOrder w, List<ProcessStageBalance> stageBalances)
//        {
//            // Build stage balances list
//            var stageBalanceDtos = new List<StageBalanceDto>();

//            if (stageBalances != null && stageBalances.Any())
//            {
//                stageBalanceDtos = stageBalances
//                    .Where(psb => psb.ProcessStage != null && psb.ProcessStage.IsActive)
//                    .OrderBy(psb => psb.ProcessStage.DisplayOrder)
//                    .Select(psb => new StageBalanceDto
//                    {
//                        ProcessStageId = psb.ProcessStageId,
//                        ProcessStageName = psb.ProcessStage.Name,
//                        DisplayOrder = psb.ProcessStage.DisplayOrder,
//                        TotalReceived = psb.TotalReceived,
//                        TotalDelivered = psb.TotalDelivered,
//                        CurrentBalance = psb.CurrentBalance,
//                        LastReceiveDate = psb.LastReceiveDate,
//                        LastDeliveryDate = psb.LastDeliveryDate
//                    })
//                    .ToList();
//            }

//            // Calculate totals from stage balances
//            var totalStageReceived = stageBalanceDtos.Sum(s => s.TotalReceived);
//            var totalStageDelivered = stageBalanceDtos.Sum(s => s.TotalDelivered);
//            var totalStageBalance = stageBalanceDtos.Sum(s => s.CurrentBalance);

//            // Calculate progress percentage
//            decimal progressPercentage = 0;
//            if (w.OrderQuantity > 0)
//            {
//                progressPercentage = Math.Round((decimal)totalStageDelivered / w.OrderQuantity * 100, 2);
//                // Cap at 100%
//                if (progressPercentage > 100)
//                    progressPercentage = 100;
//            }

//            return new WorkOrderResponseDto
//            {
//                Id = w.Id,
//                Factory = w.Factory,
//                Line = w.Line,
//                Unit = w.Unit,
//                Buyer = w.Buyer,
//                BuyerDepartment = w.BuyerDepartment,
//                StyleName = w.StyleName,
//                FastReactNo = w.FastReactNo,
//                Color = w.Color,
//                WorkOrderNo = w.WorkOrderNo,
//                WashType = w.WashType,
//                OrderQuantity = w.OrderQuantity,
//                CutQty = w.CutQty,
//                TOD = w.TOD,
//                SewingCompDate = w.SewingCompDate,
//                FirstRCVDate = w.FirstRCVDate,
//                WashApprovalDate = w.WashApprovalDate,
//                WashTargetDate = w.WashTargetDate,
//                TotalWashReceived = w.TotalWashReceived,
//                TotalWashDelivery = w.TotalWashDelivery,
//                WashBalance = w.WashBalance,
//                FromReceived = w.FromReceived,
//                Marks = w.Marks,
//                CreatedAt = w.CreatedAt,
//                UpdatedAt = w.UpdatedAt,
//                CreatedByUsername = w.CreatedByUser?.Username ?? "",
//                UpdatedByUsername = w.UpdatedByUser?.Username,

//                // Stage balances
//                StageBalances = stageBalanceDtos,
//                TotalStageReceived = totalStageReceived,
//                TotalStageDelivered = totalStageDelivered,
//                TotalStageBalance = totalStageBalance,
//                ProgressPercentage = progressPercentage
//            };
//        }

//        // ==========================================
//        // PRIVATE: HELPER METHODS
//        // ==========================================
//        private int ParseInt(string? value)
//        {
//            if (string.IsNullOrWhiteSpace(value))
//                return 0;

//            value = value.Replace(",", "").Replace(" ", "").Trim();
//            return int.TryParse(value, out int result) ? result : 0;
//        }

//        private DateTime? ParseDate(object? value)
//        {
//            if (value == null)
//                return null;

//            if (value is DateTime dateTime)
//                return dateTime;

//            string? dateString = value.ToString()?.Trim();
//            if (string.IsNullOrWhiteSpace(dateString))
//                return null;

//            string[] formats =
//            {
//                "dd-MMM-yy", "dd-MMM-yyyy", "dd/MM/yyyy",
//                "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy"
//            };

//            if (DateTime.TryParseExact(dateString, formats,
//                CultureInfo.InvariantCulture,
//                DateTimeStyles.None,
//                out DateTime parsedDate))
//            {
//                return parsedDate;
//            }

//            return DateTime.TryParse(dateString, out DateTime generalDate) ? generalDate : null;
//        }
//    }
//}

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using System.Globalization;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Extensions;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Services
{
    public class WorkOrderService : IWorkOrderService
    {
        private readonly ApplicationDbContext _context;

        public WorkOrderService(ApplicationDbContext context)
        {
            _context = context;
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // ==========================================
        // CREATE
        // ==========================================
        public async Task<WorkOrderResponseDto> CreateAsync(WorkOrderDto dto, int userId)
        {
            if (await _context.WorkOrders.AnyAsync(w => w.WorkOrderNo == dto.WorkOrderNo))
            {
                throw new InvalidOperationException($"Work Order No '{dto.WorkOrderNo}' already exists");
            }

            var workOrder = new WorkOrder
            {
                Factory = dto.Factory,
                Line = dto.Line,
                Unit = dto.Unit,
                Buyer = dto.Buyer,
                BuyerDepartment = dto.BuyerDepartment,
                StyleName = dto.StyleName,
                FastReactNo = dto.FastReactNo,
                Color = dto.Color,
                WorkOrderNo = dto.WorkOrderNo,
                WashType = dto.WashType,
                OrderQuantity = dto.OrderQuantity, // int -> int? (Safe)
                CutQty = dto.CutQty,
                TOD = dto.TOD,
                SewingCompDate = dto.SewingCompDate,
                FirstRCVDate = dto.FirstRCVDate,
                WashApprovalDate = dto.WashApprovalDate,
                WashTargetDate = dto.WashTargetDate,
                TotalWashReceived = dto.TotalWashReceived,
                TotalWashDelivery = dto.TotalWashDelivery,
                WashBalance = dto.WashBalance,
                FromReceived = dto.FromReceived,
                Marks = dto.Marks,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WorkOrders.Add(workOrder);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(workOrder.Id)
                ?? throw new Exception("Failed to retrieve created work order");
        }

        // ==========================================
        // UPDATE
        // ==========================================
        public async Task<WorkOrderResponseDto> UpdateAsync(int id, WorkOrderDto dto, int userId)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);
            if (workOrder == null)
            {
                throw new KeyNotFoundException($"Work Order with ID {id} not found");
            }

            if (workOrder.WorkOrderNo != dto.WorkOrderNo &&
                await _context.WorkOrders.AnyAsync(w => w.WorkOrderNo == dto.WorkOrderNo))
            {
                throw new InvalidOperationException($"Work Order No '{dto.WorkOrderNo}' already exists");
            }

            workOrder.Factory = dto.Factory;
            workOrder.Line = dto.Line;
            workOrder.Unit = dto.Unit;
            workOrder.Buyer = dto.Buyer;
            workOrder.BuyerDepartment = dto.BuyerDepartment;
            workOrder.StyleName = dto.StyleName;
            workOrder.FastReactNo = dto.FastReactNo;
            workOrder.Color = dto.Color;
            workOrder.WorkOrderNo = dto.WorkOrderNo;
            workOrder.WashType = dto.WashType;
            workOrder.OrderQuantity = dto.OrderQuantity; // int -> int? (Safe)
            workOrder.CutQty = dto.CutQty;
            workOrder.TOD = dto.TOD;
            workOrder.SewingCompDate = dto.SewingCompDate;
            workOrder.FirstRCVDate = dto.FirstRCVDate;
            workOrder.WashApprovalDate = dto.WashApprovalDate;
            workOrder.WashTargetDate = dto.WashTargetDate;
            workOrder.TotalWashReceived = dto.TotalWashReceived;
            workOrder.TotalWashDelivery = dto.TotalWashDelivery;
            workOrder.WashBalance = dto.WashBalance;
            workOrder.FromReceived = dto.FromReceived;
            workOrder.Marks = dto.Marks;
            workOrder.UpdatedBy = userId;
            workOrder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetByIdAsync(workOrder.Id)
                ?? throw new Exception("Failed to retrieve updated work order");
        }

        // ==========================================
        // DELETE
        // ==========================================
        public async Task<bool> DeleteAsync(int id)
        {
            var workOrder = await _context.WorkOrders.FindAsync(id);
            if (workOrder == null)
                return false;

            _context.WorkOrders.Remove(workOrder);
            await _context.SaveChangesAsync();
            return true;
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        public async Task<WorkOrderResponseDto?> GetByIdAsync(int id)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.CreatedByUser)
                .Include(w => w.UpdatedByUser)
                .FirstOrDefaultAsync(w => w.Id == id);

            if (workOrder == null)
                return null;

            var stageBalances = await _context.ProcessStageBalances
                .Include(psb => psb.ProcessStage)
                .Where(psb => psb.WorkOrderId == id)
                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
                .ToListAsync();

            return MapToResponseDto(workOrder, stageBalances);
        }

        // ==========================================
        // GET BY NO
        // ==========================================
        public async Task<WorkOrderResponseDto?> GetByWorkOrderNoAsync(string workOrderNo)
        {
            var workOrder = await _context.WorkOrders
                .Include(w => w.CreatedByUser)
                .Include(w => w.UpdatedByUser)
                .FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);

            if (workOrder == null)
                return null;

            var stageBalances = await _context.ProcessStageBalances
                .Include(psb => psb.ProcessStage)
                .Where(psb => psb.WorkOrderId == workOrder.Id)
                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
                .ToListAsync();

            return MapToResponseDto(workOrder, stageBalances);
        }

        // ==========================================
        // GET ALL
        // ==========================================
        public async Task<List<WorkOrderResponseDto>> GetAllAsync()
        {
            var workOrders = await _context.WorkOrders
                .Where(w => w.Status != 5)
                .Include(w => w.CreatedByUser)
                .Include(w => w.UpdatedByUser)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            if (!workOrders.Any())
                return new List<WorkOrderResponseDto>();

            var workOrderIds = workOrders.Select(w => w.Id).ToList();

            var allStageBalances = await _context.ProcessStageBalances
                .Include(psb => psb.ProcessStage)
                .Where(psb => workOrderIds.Contains(psb.WorkOrderId))
                .OrderBy(psb => psb.ProcessStage.DisplayOrder)
                .ToListAsync();

            var balancesByWorkOrder = allStageBalances
                .GroupBy(psb => psb.WorkOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<WorkOrderResponseDto>();
            foreach (var wo in workOrders)
            {
                var stageBalances = balancesByWorkOrder.ContainsKey(wo.Id)
                    ? balancesByWorkOrder[wo.Id]
                    : new List<ProcessStageBalance>();

                result.Add(MapToResponseDto(wo, stageBalances));
            }

            return result;
        }

        // ==========================================
        // PAGINATION
        // ==========================================
        public async Task<PaginatedResponseDto<WorkOrderResponseDto>> GetPaginatedAsync(
            PaginationRequestDto request)
        {
            try
            {
                var query = _context.WorkOrders
                    .Where(w => w.Status != 5)
                    .AsNoTracking()
                    .Include(w => w.CreatedByUser)
                    .Include(w => w.UpdatedByUser)
                    .AsQueryable();

                query = query.Search(request.SearchTerm);

                query = query.ApplyFilters(
                    request.Factory,
                    request.Buyer,
                    request.WashType,
                    request.Line,
                    request.Unit,
                    request.FromDate,
                    request.ToDate
                );

                query = query.ApplySort(request.SortBy, request.SortOrder);

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                var skip = (request.Page - 1) * request.PageSize;
                var workOrders = await query
                    .Skip(skip)
                    .Take(request.PageSize)
                    .ToListAsync();

                if (!workOrders.Any())
                {
                    return new PaginatedResponseDto<WorkOrderResponseDto>
                    {
                        Success = true,
                        Message = "No records found",
                        Data = new List<WorkOrderResponseDto>(),
                        Pagination = new PaginationMetadata
                        {
                            CurrentPage = request.Page,
                            PageSize = request.PageSize,
                            TotalRecords = 0,
                            TotalPages = 0,
                            HasPrevious = false,
                            HasNext = false
                        }
                    };
                }

                var workOrderIds = workOrders
                    .Select(w => w.Id)
                    .ToList();

                var allStageBalances = await _context.ProcessStageBalances
                    .AsNoTracking()
                    .Include(psb => psb.ProcessStage)
                    .Where(psb => workOrderIds.Contains(psb.WorkOrderId))
                    .OrderBy(psb => psb.ProcessStage.DisplayOrder)
                    .ToListAsync();

                var balancesByWorkOrder = allStageBalances
                    .GroupBy(psb => psb.WorkOrderId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                var data = new List<WorkOrderResponseDto>();
                foreach (var wo in workOrders)
                {
                    var stageBalances = balancesByWorkOrder.ContainsKey(wo.Id)
                        ? balancesByWorkOrder[wo.Id]
                        : new List<ProcessStageBalance>();

                    data.Add(MapToResponseDto(wo, stageBalances));
                }

                return new PaginatedResponseDto<WorkOrderResponseDto>
                {
                    Success = true,
                    Message = null,
                    Data = data,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = request.Page > 1,
                        HasNext = request.Page < totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                return new PaginatedResponseDto<WorkOrderResponseDto>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Data = new List<WorkOrderResponseDto>(),
                    Pagination = new PaginationMetadata()
                };
            }
        }

        // ==========================================
        // BULK UPLOAD
        // ==========================================

        public async Task<WorkOrderBulkUploadResponseDto> BulkUploadFromExcelAsync(IFormFile file, int userId)
        {
            var response = new WorkOrderBulkUploadResponseDto { Success = false };

            try
            {
                if (file == null || file.Length == 0)
                {
                    response.Message = "No file uploaded";
                    return response;
                }

                if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
                {
                    response.Message = "Invalid file format. Please upload Excel file (.xlsx or .xls)";
                    return response;
                }

                using var stream = new MemoryStream();
                await file.CopyToAsync(stream);

                using var package = new ExcelPackage(stream);
                var worksheet = package.Workbook.Worksheets[0];
                var rowCount = worksheet.Dimension?.Rows ?? 0;
                var colCount = worksheet.Dimension?.Columns ?? 0;

                if (rowCount < 2)
                {
                    response.Message = "Excel file has no data rows";
                    return response;
                }

                // ✅ DYNAMIC COLUMN MAPPING - Find columns by header name
                var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                for (int col = 1; col <= colCount; col++)
                {
                    var headerValue = worksheet.Cells[1, col].Value?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(headerValue))
                    {
                        columnMap[headerValue] = col;
                    }
                }

                // ✅ Define expected column names (case-insensitive)
                int GetColumn(params string[] possibleNames)
                {
                    foreach (var name in possibleNames)
                    {
                        if (columnMap.TryGetValue(name, out int col))
                            return col;
                    }
                    return -1; // Not found
                }

                // Map columns by header names
                int colFactory = GetColumn("Factory");
                int colLine = GetColumn("Line");
                int colUnit = GetColumn("Unit");
                int colBuyer = GetColumn("Buyer");
                int colBuyerDept = GetColumn("Buyer Department", "BuyerDepartment", "Buyer Dept");
                int colStyleName = GetColumn("Style Name", "StyleName", "Style");
                int colFastReactNo = GetColumn("FastReact No", "FastReactNo", "Fast React No", "FastReact");
                int colColor = GetColumn("Color", "Colour");
                int colWorkOrderNo = GetColumn("Work Order No", "WorkOrderNo", "WO No", "WO");
                int colWashType = GetColumn("Wash Type", "WashType", "Wash");
                int colOrderQty = GetColumn("Order Quantity", "OrderQuantity", "Order Qty", "Ord Qty");
                int colCutQty = GetColumn("Cut Qty", "CutQty", "Cut Quantity");
                int colTOD = GetColumn("TOD");
                int colSewingCompDate = GetColumn("Sewing Comp. Date", "SewingCompDate", "Sewing Comp Date");
                int colFirstRCVDate = GetColumn("1st RCV Date", "FirstRCVDate", "First RCV Date", "1st Receive Date");
                int colWashApprovalDate = GetColumn("Wash Approval Date", "WashApprovalDate");
                int colWashTargetDate = GetColumn("Wash Target Date", "WashTargetDate");
                int colTotalWashReceived = GetColumn("Total Wash Received", "TotalWashReceived");
                int colTotalWashDelivery = GetColumn("Total Wash Delivery", "TotalWashDelivery");
                int colWashBalance = GetColumn("Wash Balance", "WashBalance");
                int colFromReceived = GetColumn("From Received", "FromReceived");
                int colMarks = GetColumn("Marks", "Remarks", "Notes");

                // ✅ Validate required columns
                if (colWorkOrderNo == -1)
                {
                    response.Message = "Required column 'Work Order No' not found in Excel file";
                    response.Errors.Add($"Found columns: {string.Join(", ", columnMap.Keys)}");
                    return response;
                }

                // ✅ Log column mapping for debugging
                response.Errors.Add($"Column Mapping - WashType: Col{colWashType}, FastReactNo: Col{colFastReactNo}");

                int actualDataRows = 0;
                for (int row = 2; row <= rowCount; row++)
                {
                    var workOrderNo = colWorkOrderNo > 0
                        ? worksheet.Cells[row, colWorkOrderNo].Value?.ToString()?.Trim()
                        : null;
                    if (!string.IsNullOrEmpty(workOrderNo))
                        actualDataRows++;
                }

                response.TotalRecords = actualDataRows;

                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var workOrderNo = colWorkOrderNo > 0
                            ? worksheet.Cells[row, colWorkOrderNo].Value?.ToString()?.Trim()
                            : null;

                        if (string.IsNullOrEmpty(workOrderNo))
                            continue;

                        var existingWorkOrder = await _context.WorkOrders
                            .FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);

                        var workOrder = existingWorkOrder ?? new WorkOrder();

                        // ✅ Use dynamic column mapping
                        workOrder.Factory = GetCellValue(worksheet, row, colFactory);
                        workOrder.Line = GetCellValue(worksheet, row, colLine);
                        workOrder.Unit = GetCellValue(worksheet, row, colUnit);
                        workOrder.Buyer = GetCellValue(worksheet, row, colBuyer);
                        workOrder.BuyerDepartment = GetCellValue(worksheet, row, colBuyerDept);
                        workOrder.StyleName = GetCellValue(worksheet, row, colStyleName);
                        workOrder.FastReactNo = GetCellValue(worksheet, row, colFastReactNo);
                        workOrder.Color = GetCellValue(worksheet, row, colColor);
                        workOrder.WorkOrderNo = workOrderNo;
                        workOrder.WashType = GetCellValue(worksheet, row, colWashType);

                        workOrder.OrderQuantity = ParseInt(GetCellValue(worksheet, row, colOrderQty));
                        workOrder.CutQty = ParseInt(GetCellValue(worksheet, row, colCutQty));

                        workOrder.TOD = ParseDate(GetCellObject(worksheet, row, colTOD));
                        workOrder.SewingCompDate = ParseDate(GetCellObject(worksheet, row, colSewingCompDate));
                        workOrder.FirstRCVDate = ParseDate(GetCellObject(worksheet, row, colFirstRCVDate));
                        workOrder.WashApprovalDate = ParseDate(GetCellObject(worksheet, row, colWashApprovalDate));
                        workOrder.WashTargetDate = ParseDate(GetCellObject(worksheet, row, colWashTargetDate));

                        workOrder.TotalWashReceived = ParseInt(GetCellValue(worksheet, row, colTotalWashReceived));
                        workOrder.TotalWashDelivery = ParseInt(GetCellValue(worksheet, row, colTotalWashDelivery));
                        workOrder.WashBalance = ParseInt(GetCellValue(worksheet, row, colWashBalance));

                        if (workOrder.WashBalance == 0 &&
                            (workOrder.TotalWashReceived > 0 || workOrder.TotalWashDelivery > 0))
                        {
                            workOrder.WashBalance = workOrder.TotalWashReceived.GetValueOrDefault()
                                                  - workOrder.TotalWashDelivery.GetValueOrDefault();
                        }

                        workOrder.FromReceived = ParseInt(GetCellValue(worksheet, row, colFromReceived));
                        workOrder.Marks = GetCellValue(worksheet, row, colMarks);

                        if (existingWorkOrder != null)
                        {
                            workOrder.UpdatedBy = userId;
                            workOrder.UpdatedAt = DateTime.UtcNow;
                            response.UpdatedCount++;
                        }
                        else
                        {
                            workOrder.CreatedBy = userId;
                            workOrder.CreatedAt = DateTime.UtcNow;
                            _context.WorkOrders.Add(workOrder);
                            response.SuccessCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        var workOrderNo = colWorkOrderNo > 0
                            ? worksheet.Cells[row, colWorkOrderNo].Value?.ToString()?.Trim()
                            : "Unknown";

                        if (!string.IsNullOrEmpty(workOrderNo))
                        {
                            response.FailedCount++;
                            response.Errors.Add($"Row {row} (WO: {workOrderNo}): {ex.Message}");
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // ✅ Clear debug errors if successful
                response.Errors = response.Errors.Where(e => !e.StartsWith("DEBUG") && !e.StartsWith("Column Mapping")).ToList();

                response.Success = true;
                response.Message = $"Bulk upload completed. Success: {response.SuccessCount}, Updated: {response.UpdatedCount}, Failed: {response.FailedCount}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error processing file: {ex.Message}";
            }

            return response;
        }

        // ✅ Helper methods
        private string GetCellValue(ExcelWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return "";
            return worksheet.Cells[row, col].Value?.ToString()?.Trim() ?? "";
        }

        private object? GetCellObject(ExcelWorksheet worksheet, int row, int col)
        {
            if (col <= 0) return null;
            return worksheet.Cells[row, col].Value;
        }

        //public async Task<WorkOrderBulkUploadResponseDto> BulkUploadFromExcelAsync(IFormFile file, int userId)
        //{
        //    var response = new WorkOrderBulkUploadResponseDto { Success = false };

        //    try
        //    {
        //        if (file == null || file.Length == 0)
        //        {
        //            response.Message = "No file uploaded";
        //            return response;
        //        }

        //        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        //        {
        //            response.Message = "Invalid file format. Please upload Excel file (.xlsx or .xls)";
        //            return response;
        //        }

        //        using var stream = new MemoryStream();
        //        await file.CopyToAsync(stream);

        //        using var package = new ExcelPackage(stream);
        //        var worksheet = package.Workbook.Worksheets[0];
        //        var rowCount = worksheet.Dimension?.Rows ?? 0;

        //        if (rowCount < 2)
        //        {
        //            response.Message = "Excel file has no data rows";
        //            return response;
        //        }

        //        int actualDataRows = 0;
        //        for (int row = 2; row <= rowCount; row++)
        //        {
        //            var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
        //            if (!string.IsNullOrEmpty(workOrderNo))
        //                actualDataRows++;
        //        }

        //        response.TotalRecords = actualDataRows;

        //        for (int row = 2; row <= rowCount; row++)
        //        {
        //            try
        //            {
        //                var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();

        //                if (string.IsNullOrEmpty(workOrderNo))
        //                    continue;

        //                var existingWorkOrder = await _context.WorkOrders
        //                    .FirstOrDefaultAsync(w => w.WorkOrderNo == workOrderNo);

        //                var workOrder = existingWorkOrder ?? new WorkOrder();

        //                workOrder.Factory = worksheet.Cells[row, 1].Value?.ToString()?.Trim() ?? "";
        //                workOrder.Line = worksheet.Cells[row, 2].Value?.ToString()?.Trim() ?? "";
        //                workOrder.Unit = worksheet.Cells[row, 3].Value?.ToString()?.Trim() ?? "";
        //                workOrder.Buyer = worksheet.Cells[row, 4].Value?.ToString()?.Trim() ?? "";
        //                workOrder.BuyerDepartment = worksheet.Cells[row, 5].Value?.ToString()?.Trim() ?? "";
        //                workOrder.StyleName = worksheet.Cells[row, 6].Value?.ToString()?.Trim() ?? "";
        //                workOrder.FastReactNo = worksheet.Cells[row, 7].Value?.ToString()?.Trim() ?? "";
        //                workOrder.Color = worksheet.Cells[row, 8].Value?.ToString()?.Trim() ?? "";
        //                workOrder.WorkOrderNo = workOrderNo;
        //                workOrder.WashType = worksheet.Cells[row, 10].Value?.ToString()?.Trim() ?? "";

        //                workOrder.OrderQuantity = ParseInt(worksheet.Cells[row, 11].Value?.ToString());
        //                workOrder.CutQty = ParseInt(worksheet.Cells[row, 12].Value?.ToString());

        //                workOrder.TOD = ParseDate(worksheet.Cells[row, 13].Value);
        //                workOrder.SewingCompDate = ParseDate(worksheet.Cells[row, 14].Value);
        //                workOrder.FirstRCVDate = ParseDate(worksheet.Cells[row, 15].Value);
        //                workOrder.WashApprovalDate = ParseDate(worksheet.Cells[row, 16].Value);
        //                workOrder.WashTargetDate = ParseDate(worksheet.Cells[row, 17].Value);

        //                workOrder.TotalWashReceived = ParseInt(worksheet.Cells[row, 18].Value?.ToString());
        //                workOrder.TotalWashDelivery = ParseInt(worksheet.Cells[row, 19].Value?.ToString());

        //                var washBalanceStr = worksheet.Cells[row, 20].Value?.ToString()?.Trim();
        //                workOrder.WashBalance = ParseInt(washBalanceStr);

        //                if (workOrder.WashBalance == 0 &&
        //                    (workOrder.TotalWashReceived > 0 || workOrder.TotalWashDelivery > 0))
        //                {
        //                    // Nullable int math needs .GetValueOrDefault()
        //                    workOrder.WashBalance = workOrder.TotalWashReceived.GetValueOrDefault() - workOrder.TotalWashDelivery.GetValueOrDefault();
        //                }

        //                var col21Value = worksheet.Cells[row, 21].Value?.ToString()?.Trim();
        //                if (!string.IsNullOrEmpty(col21Value) &&
        //                    int.TryParse(col21Value.Replace(",", ""), out _))
        //                {
        //                    workOrder.FromReceived = ParseInt(col21Value);
        //                    workOrder.Marks = worksheet.Cells[row, 22].Value?.ToString()?.Trim();
        //                }
        //                else
        //                {
        //                    workOrder.FromReceived = 0;
        //                    workOrder.Marks = col21Value;
        //                }

        //                if (existingWorkOrder != null)
        //                {
        //                    workOrder.UpdatedBy = userId;
        //                    workOrder.UpdatedAt = DateTime.UtcNow;
        //                    response.UpdatedCount++;
        //                }
        //                else
        //                {
        //                    workOrder.CreatedBy = userId;
        //                    workOrder.CreatedAt = DateTime.UtcNow;
        //                    _context.WorkOrders.Add(workOrder);
        //                    response.SuccessCount++;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                var workOrderNo = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
        //                if (!string.IsNullOrEmpty(workOrderNo))
        //                {
        //                    response.FailedCount++;
        //                    response.Errors.Add($"Row {row} (WO: {workOrderNo}): {ex.Message}");
        //                }
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        response.Success = true;
        //        response.Message = $"Bulk upload completed. Success: {response.SuccessCount}, Updated: {response.UpdatedCount}, Failed: {response.FailedCount}";
        //    }
        //    catch (Exception ex)
        //    {
        //        response.Success = false;
        //        response.Message = $"Error processing file: {ex.Message}";
        //    }

        //    return response;
        //}




        // ==========================================
        // MAP TO DTO (CRITICAL FIXES)
        // ==========================================
        private WorkOrderResponseDto MapToResponseDto(WorkOrder w, List<ProcessStageBalance> stageBalances)
        {
            var stageBalanceDtos = new List<StageBalanceDto>();

            if (stageBalances != null && stageBalances.Any())
            {
                stageBalanceDtos = stageBalances
                    .Where(psb => psb.ProcessStage != null && psb.ProcessStage.IsActive)
                    .OrderBy(psb => psb.ProcessStage.DisplayOrder)
                    .Select(psb => new StageBalanceDto
                    {
                        ProcessStageId = psb.ProcessStageId,
                        ProcessStageName = psb.ProcessStage.Name,
                        DisplayOrder = psb.ProcessStage.DisplayOrder,
                        TotalReceived = psb.TotalReceived,
                        TotalDelivered = psb.TotalDelivered,
                        CurrentBalance = psb.CurrentBalance,
                        LastReceiveDate = psb.LastReceiveDate,
                        LastDeliveryDate = psb.LastDeliveryDate
                    })
                    .ToList();
            }

            var totalStageReceived = stageBalanceDtos.Sum(s => s.TotalReceived);
            var totalStageDelivered = stageBalanceDtos.Sum(s => s.TotalDelivered);
            var totalStageBalance = stageBalanceDtos.Sum(s => s.CurrentBalance);

            // ✅ FIX: Extract OrderQuantity safely as int
            int orderQty = w.OrderQuantity.GetValueOrDefault();
            decimal progressPercentage = 0;

            if (orderQty > 0)
            {
                // ✅ FIX: Ensure decimal conversion is safe
                progressPercentage = Math.Round((decimal)totalStageDelivered / orderQty * 100, 2);
                if (progressPercentage > 100)
                    progressPercentage = 100;
            }

            return new WorkOrderResponseDto
            {
                Id = w.Id,
                Factory = w.Factory,
                Line = w.Line,
                Unit = w.Unit,
                Buyer = w.Buyer,
                BuyerDepartment = w.BuyerDepartment,
                StyleName = w.StyleName,
                FastReactNo = w.FastReactNo,
                Color = w.Color,
                WorkOrderNo = w.WorkOrderNo,
                WashType = w.WashType, 
                OrderQuantity = w.OrderQuantity.GetValueOrDefault(),
                CutQty = w.CutQty.GetValueOrDefault(),
                TOD = w.TOD,
                SewingCompDate = w.SewingCompDate,
                FirstRCVDate = w.FirstRCVDate,
                WashApprovalDate = w.WashApprovalDate,
                WashTargetDate = w.WashTargetDate, 
                TotalWashReceived = w.TotalWashReceived.GetValueOrDefault(),
                TotalWashDelivery = w.TotalWashDelivery.GetValueOrDefault(),
                WashBalance = w.WashBalance.GetValueOrDefault(),
                FromReceived = w.FromReceived.GetValueOrDefault(),

                Marks = w.Marks,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt,
                CreatedByUsername = w.CreatedByUser?.Username ?? "",
                UpdatedByUsername = w.UpdatedByUser?.Username,
                StageBalances = stageBalanceDtos,
                TotalStageReceived = totalStageReceived,
                TotalStageDelivered = totalStageDelivered,
                TotalStageBalance = totalStageBalance,
                ProgressPercentage = progressPercentage
            };
        }

        private int ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Replace(",", "").Replace(" ", "").Trim();
            return int.TryParse(value, out int result) ? result : 0;
        }

        private DateTime? ParseDate(object? value)
        {
            if (value == null)
                return null;

            if (value is DateTime dateTime)
                return dateTime;

            string? dateString = value.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(dateString))
                return null;

            string[] formats =
            {
                "dd-MMM-yy", "dd-MMM-yyyy", "dd/MM/yyyy",
                "yyyy-MM-dd", "dd-MM-yyyy", "MM/dd/yyyy"
            }; 

            if (DateTime.TryParseExact(dateString, formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime parsedDate))
            {
                return parsedDate;
            }

            return DateTime.TryParse(dateString, out DateTime generalDate) ? generalDate : null;
        }
    }
}