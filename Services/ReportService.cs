// D:\test c#\wsahRecieveDelivary\Services\ReportService.cs
using Microsoft.EntityFrameworkCore;
using System.Text;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.Models;
using wsahRecieveDelivary.Models.Enums;

namespace wsahRecieveDelivary.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITusukaExtremeRepository _tusukaExtremeRepo;

        public ReportService(ApplicationDbContext context, ITusukaExtremeRepository tusukaExtremeRepo)
        {
            _context = context;
            _tusukaExtremeRepo = tusukaExtremeRepo;
        }

        
        // ==========================================
        // INTERNAL DTO FOR WORK ORDER DATA
        // ==========================================
        private class WorkOrderData
        {
            public int Id { get; set; }
            public string Factory { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public string WorkOrderNo { get; set; } = string.Empty;
            public string? FastReactNo { get; set; }
            public string Buyer { get; set; } = string.Empty;
            public string StyleName { get; set; } = string.Empty;
            public string? Marks { get; set; }
            public int OrderQuantity { get; set; }
            public DateTime? WashTargetDate { get; set; }
            public int TotalWashReceived { get; set; }
            public int TotalWashDelivery { get; set; }
            public int? Status { get; set; }
        }

        // ==========================================
        // INTERNAL DTO FOR TRANSACTION DATA
        // ==========================================
        private class TransactionData
        {
            public int WorkOrderId { get; set; }
            public int ProcessStageId { get; set; }
            public string StageName { get; set; } = string.Empty;
            public TransactionType TransactionType { get; set; }
            public decimal Quantity { get; set; }

            // ✅ NEW: Shift-related fields
            public DateOnly ShiftDate { get; set; }
            public ShiftType ShiftType { get; set; }

        }

        // ==========================================
        // GET TRANSACTION REPORT (MAIN METHOD)
        // ==========================================
        public async Task<ReportResponseDto> GetTransactionReportAsync(ReportRequestDto request)
        {
            try
            {
                if (request.Page < 1) request.Page = 1;
                if (request.PageSize < 1) request.PageSize = 25;
                if (request.PageSize > 100) request.PageSize = 100;

                Console.WriteLine("📊 GetTransactionReportAsync called");
                Console.WriteLine($"   Page: {request.Page}, PageSize: {request.PageSize}");
                Console.WriteLine($"   Buyer: {request.Buyer}, Factory: {request.Factory}");
                Console.WriteLine($"   StartDate: {request.StartDate}, EndDate: {request.EndDate}");
                Console.WriteLine($"   ShiftType: {request.ShiftType}");

                var workOrderQuery = _context.WorkOrders
                    .Where(x=> x.TotalWashReceived > 0)
                    .AsNoTracking()
                    .AsQueryable();

                workOrderQuery = ApplyWorkOrderFilters(workOrderQuery, request);
                if (request.IsCompleted == true)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status == 5);
                }
                else if (request.IsCompleted == false)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status != 5 || w.Status == null);
                }



                if (request.StartDate.HasValue || request.EndDate.HasValue ||
                    request.ProcessStageId.HasValue || request.TransactionTypeId.HasValue ||
                    request.ShiftType.HasValue)
                {
                    var transactionQuery = _context.WashTransactions
                        .AsNoTracking()
                        .Where(t => t.IsActive);

                    if (request.StartDate.HasValue)
                        transactionQuery = transactionQuery.Where(t => t.ShiftDate >= request.StartDate);

                    if (request.EndDate.HasValue)
                        transactionQuery = transactionQuery.Where(t => t.ShiftDate <= request.EndDate);

                    if (request.ProcessStageId.HasValue)
                        transactionQuery = transactionQuery.Where(t => t.ProcessStageId == request.ProcessStageId.Value);

                    if (request.TransactionTypeId.HasValue)
                    {
                        var transactionType = (TransactionType)request.TransactionTypeId.Value;
                        transactionQuery = transactionQuery.Where(t => t.TransactionType == transactionType);
                    }

                    if (request.ShiftType.HasValue)
                    {
                        var shiftType = (ShiftType)request.ShiftType.Value;
                        transactionQuery = transactionQuery.Where(t => t.ShiftType == shiftType);
                        Console.WriteLine($"   Filtering by ShiftType: {shiftType}");
                    }

                   

                    var matchingWorkOrderIds = await transactionQuery
                        .Select(t => t.WorkOrderId)
                        .Distinct()
                        .ToListAsync();

                    workOrderQuery = workOrderQuery.Where(w => matchingWorkOrderIds.Contains(w.Id));
                }

                var totalCount = await workOrderQuery.CountAsync();
                Console.WriteLine($"   Total work orders matching filters: {totalCount}");

                if (totalCount == 0)
                {
                    return new ReportResponseDto
                    {
                        Success = true,
                        Message = "No data found",
                        Data = new List<ReportRowDto>(),
                        Pagination = new PaginationMetadata
                        {
                            CurrentPage = request.Page,
                            PageSize = request.PageSize,
                            TotalRecords = 0,
                            TotalPages = 0,
                            HasPrevious = false,
                            HasNext = false
                        },
                        Summary = new ReportSummaryDto(),
                        FilterOptions = await GetFilterOptionsAsync()
                    };
                }

                workOrderQuery = ApplySorting(workOrderQuery, request.SortBy, request.SortOrder);

                var workOrders = await workOrderQuery
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(w => new WorkOrderData
                    {
                        Id = w.Id,
                        Factory = w.Factory ?? "",
                        Unit = w.Unit ?? "",
                        WorkOrderNo = w.WorkOrderNo ?? "",
                        FastReactNo = w.FastReactNo,
                        Buyer = w.Buyer ?? "",
                        StyleName = w.StyleName ?? "",
                        Marks = w.Marks,
                        OrderQuantity = w.OrderQuantity ?? 0,
                        WashTargetDate = w.WashTargetDate,
                        TotalWashReceived = w.TotalWashReceived ?? 0,
                        TotalWashDelivery = w.TotalWashDelivery ?? 0,
                        Status = (int?)w.Status
                    })
                    .ToListAsync();

                Console.WriteLine($"   Fetched {workOrders.Count} work orders for current page");

                var workOrderIds = workOrders.Select(w => w.Id).ToList();

                var transactionsForPage = await GetTransactionsForWorkOrders(
                    workOrderIds,
                    request.StartDate,
                    request.EndDate,
                    request.ProcessStageId,
                    request.TransactionTypeId,
                    request.ShiftType
                );

                var reportData = BuildReportRows(workOrders, transactionsForPage);
                var summary = await CalculateSummaryAsync(request);
                var filterOptions = await GetFilterOptionsAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                Console.WriteLine($"✅ Report generated successfully");

                return new ReportResponseDto
                {
                    Success = true,
                    Data = reportData,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = request.Page > 1,
                        HasNext = request.Page < totalPages
                    },
                    Summary = summary,
                    FilterOptions = filterOptions
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetTransactionReportAsync: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                return new ReportResponseDto
                {
                    Success = false,
                    Message = $"Error generating report: {ex.Message}",
                    FilterOptions = await GetFilterOptionsAsync()
                };
            }
        }

        // ==========================================
        // GET SUMMARY ONLY
        // ==========================================
        public async Task<ReportSummaryDto> GetSummaryAsync(ReportRequestDto request)
        {
            return await CalculateSummaryAsync(request);
        }

        // ==========================================
        // EXPORT TO CSV
        // ==========================================
        public async Task<byte[]> ExportToCsvAsync(ReportRequestDto request)
        {
            try
            {
                Console.WriteLine("📥 ExportToCsvAsync called");

                // Build query
                var workOrderQuery = _context.WorkOrders.AsNoTracking().AsQueryable();
                workOrderQuery = ApplyWorkOrderFilters(workOrderQuery, request);
                if (request.IsCompleted == true)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status == 5);
                }
                else if (request.IsCompleted == false)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status != 5 || w.Status == null);
                }



                // Apply transaction filters if needed
                if (request.StartDate.HasValue || request.EndDate.HasValue ||
                    request.ProcessStageId.HasValue || request.TransactionTypeId.HasValue)
                {
                    var transactionQuery = _context.WashTransactions
                        .AsNoTracking()
                        .Where(t => t.IsActive);

                    //if (request.StartDate.HasValue)
                    //    transactionQuery = transactionQuery.Where(t => t.TransactionDate.Date >= request.StartDate);

                    //if (request.EndDate.HasValue)
                    //    transactionQuery = transactionQuery.Where(t => t.TransactionDate.Date <= request.EndDate);
                    if (request.StartDate.HasValue)
                    {
                        var startDate = request.StartDate;

                        transactionQuery = transactionQuery
                            .Where(t => t.ShiftDate >= startDate);
                    }

                    if (request.EndDate.HasValue)
                    {
                        var endDate = request.EndDate;

                        transactionQuery = transactionQuery
                            .Where(t => t.ShiftDate <= endDate);
                    }

                    if (request.ProcessStageId.HasValue)
                        transactionQuery = transactionQuery.Where(t => t.ProcessStageId == request.ProcessStageId.Value);

                    if (request.TransactionTypeId.HasValue)
                    {
                        var transactionType = (TransactionType)request.TransactionTypeId.Value;
                        transactionQuery = transactionQuery.Where(t => t.TransactionType == transactionType);
                    }

                    var matchingWorkOrderIds = await transactionQuery
                        .Select(t => t.WorkOrderId)
                        .Distinct()
                        .ToListAsync();

                    workOrderQuery = workOrderQuery.Where(w => matchingWorkOrderIds.Contains(w.Id));
                }

                workOrderQuery = ApplySorting(workOrderQuery, request.SortBy, request.SortOrder);

                var workOrders = await workOrderQuery
                    .Select(w => new WorkOrderData
                    {
                        Id = w.Id,
                        Factory = w.Factory ?? "",
                        Unit = w.Unit ?? "",
                        WorkOrderNo = w.WorkOrderNo ?? "",
                        FastReactNo = w.FastReactNo,
                        Buyer = w.Buyer ?? "",
                        StyleName = w.StyleName ?? "",
                        Marks = w.Marks,
                        OrderQuantity = w.OrderQuantity ?? 0,
                        WashTargetDate = w.WashTargetDate,
                        TotalWashReceived = w.TotalWashReceived ?? 0,
                        TotalWashDelivery = w.TotalWashDelivery ?? 0,
                        Status = (int?)w.Status
                    })
                    .ToListAsync();

                if (!workOrders.Any())
                {
                    throw new Exception("No data to export");
                }

                var workOrderIds = workOrders.Select(w => w.Id).ToList();

                var transactions = await GetTransactionsForWorkOrders(
                    workOrderIds,
                    request.StartDate,
                    request.EndDate,
                    request.ProcessStageId,
                    request.TransactionTypeId,
                    request.ShiftType
                );

                var reportData = BuildReportRows(workOrders, transactions);

                // Get all unique stage names
                var allStageNames = await _context.ProcessStages
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => s.Name)
                    .ToListAsync();

                // Build CSV
                var sb = new StringBuilder();

                // Add BOM for UTF-8
                // sb.Append('\uFEFF');

                // Add filter info
                sb.AppendLine("FILTER CRITERIA");
                if (!string.IsNullOrEmpty(request.Buyer)) sb.AppendLine($"Buyer,\"{request.Buyer}\"");
                if (!string.IsNullOrEmpty(request.Factory)) sb.AppendLine($"Factory,\"{request.Factory}\"");
                if (!string.IsNullOrEmpty(request.Unit)) sb.AppendLine($"Unit,\"{request.Unit}\"");
                if (request.StartDate.HasValue) sb.AppendLine($"Start Date,{request.StartDate.Value:yyyy-MM-dd}");
                if (request.EndDate.HasValue) sb.AppendLine($"End Date,{request.EndDate.Value:yyyy-MM-dd}");
                if (request.WashTargetStartDate.HasValue) sb.AppendLine($"Wash Target Start,{request.WashTargetStartDate.Value:yyyy-MM-dd}");
                if (request.WashTargetEndDate.HasValue) sb.AppendLine($"Wash Target End,{request.WashTargetEndDate.Value:yyyy-MM-dd}");
                sb.AppendLine();

                // Add summary
                var summary = await CalculateSummaryAsync(request);
                sb.AppendLine("SUMMARY");
                sb.AppendLine($"Total Work Orders,{summary.TotalWorkOrders}");
                sb.AppendLine($"Total Transactions,{summary.TotalTransactions}");
                sb.AppendLine($"Total Receive Qty,{summary.TotalReceiveQty}");
                sb.AppendLine($"Total Delivery Qty,{summary.TotalDeliveryQty}");
                sb.AppendLine($"Balance,{summary.Balance}");
                sb.AppendLine($"Total Order Quantity,{summary.TotalOrderQuantity}");
                sb.AppendLine();

                // Stage breakdown
                if (summary.StageBreakdown.Any())
                {
                    sb.AppendLine("STAGE BREAKDOWN");
                    sb.AppendLine("Stage Name,Receive,Delivery,Balance");
                    foreach (var stage in summary.StageBreakdown)
                    {
                        sb.AppendLine($"\"{stage.Key}\",{stage.Value.Receive},{stage.Value.Delivery},{stage.Value.Balance}");
                    }
                    sb.AppendLine();
                }

                // Headers
                var headers = new List<string>
                {
                    "Factory", "Unit", "Work Order No", "FastReact No", "Buyer", "Style Name",
                    "Order Qty", "Wash Target Date", "Marks", "Total Wash Received", "Total Wash Delivery", "Status"
                };

                // Add stage headers
                foreach (var stageName in allStageNames)
                {
                    headers.Add($"{stageName} Rcv");
                    headers.Add($"{stageName} Del");
                }

                sb.AppendLine("DETAILED DATA");
                sb.AppendLine(string.Join(",", headers.Select(h => $"\"{h}\"")));

                // Data rows
                foreach (var row in reportData)
                {
                    var values = new List<string>
                    {
                        $"\"{EscapeCsvField(row.Factory)}\"",
                        $"\"{EscapeCsvField(row.Unit)}\"",
                        $"=\"{EscapeCsvField(row.WorkOrderNo)}\"",
                        $"=\"{EscapeCsvField(row.FastReactNo)}\"",
                        $"\"{EscapeCsvField(row.Buyer)}\"",
                        $"\"{EscapeCsvField(row.StyleName)}\"",
                        row.OrderQuantity.ToString(),
                        row.WashTargetDate.HasValue ? row.WashTargetDate.Value.ToString("yyyy-MM-dd") : "",
                        $"\"{EscapeCsvField(row.Marks ?? "")}\"",
                        row.TotalWashReceived.ToString(),
                        row.TotalWashDelivery.ToString(),
                        row.Status == 5 ? "Completed" : "Not Completed"
                    };

                    // Add stage values
                    foreach (var stageName in allStageNames)
                    {
                        if (row.StageQuantities.TryGetValue(stageName, out var stageQty))
                        {
                            values.Add(stageQty.Receive.ToString());
                            values.Add(stageQty.Delivery.ToString());
                        }
                        else
                        {
                            values.Add("0");
                            values.Add("0");
                        }
                    }

                    sb.AppendLine(string.Join(",", values));
                }

                Console.WriteLine($"✅ CSV export completed - {reportData.Count} records");

                // Return with BOM for proper UTF-8 encoding in Excel
                var preamble = Encoding.UTF8.GetPreamble();
                var content = Encoding.UTF8.GetBytes(sb.ToString());
                var result = new byte[preamble.Length + content.Length];
                preamble.CopyTo(result, 0);
                content.CopyTo(result, preamble.Length);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Export error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                throw;
            }
        }

        // ==========================================
        // GET USER TRANSACTION HISTORY
        // ==========================================
        public async Task<List<UserTransactionHistoryDto>> GetUserTransactionHistoryAsync(
            int userId, 
            DateTime? startDate, 
            DateTime? endDate)
        {
            var query = _context.WashTransactions
                .AsNoTracking()
                .Where(t => t.CreatedBy == userId && t.IsActive);

            if (startDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate.Date >= startDate.Value.Date);
            }

            if (endDate.HasValue)
            {
                query = query.Where(t => t.TransactionDate.Date <= endDate.Value.Date);
            }

            // Optimization: Select only needed fields, no Include needed
            return await query
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.Id)
                .Select(t => new UserTransactionHistoryDto
                {
                    TransactionId = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo ?? "-",
                    StyleName = t.WorkOrder.StyleName ?? "-",
                    Buyer = t.WorkOrder.Buyer ?? "-",
                    Factory = t.WorkOrder.Factory ?? "-",
                    Unit = t.WorkOrder.Unit ?? "-",
                    FastReactNo = t.WorkOrder.FastReactNo,
                    WashTargetDate = t.WorkOrder.WashTargetDate,
                    StageName = t.ProcessStage.Name,
                    TransactionType = t.TransactionType.ToString(),
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET USER WORK ORDER SUMMARY
        // ==========================================
        public async Task<List<UserWorkOrderSummaryDto>> GetUserWorkOrderSummaryAsync(
            int userId,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? buyer = null,
            string? factory = null,
            string? unit = null,
            int? processStageId = null)
        {
            // 1. Transaction Query
            var transactionQuery = _context.WashTransactions
                .AsNoTracking()
                .Include(t => t.ProcessStage) // Include Stage info
                .Where(t => t.CreatedBy == userId && t.IsActive);

            if (processStageId.HasValue)
                transactionQuery = transactionQuery.Where(t => t.ProcessStageId == processStageId.Value);

            if (startDate.HasValue)
                transactionQuery = transactionQuery.Where(t => t.TransactionDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                transactionQuery = transactionQuery.Where(t => t.TransactionDate.Date <= endDate.Value.Date);

            // 2. Group by WorkOrder, Stage, and Type to get granular data
            var rawData = await transactionQuery
                .GroupBy(t => new { t.WorkOrderId, t.ProcessStageId, t.ProcessStage.Name, t.ProcessStage.DisplayOrder, t.TransactionType })
                .Select(g => new
                {
                    WorkOrderId = g.Key.WorkOrderId,
                    StageName = g.Key.Name,
                    DisplayOrder = g.Key.DisplayOrder,
                    TransactionType = g.Key.TransactionType,
                    Quantity = g.Sum(t => t.Quantity),
                    LastDate = g.Max(t => t.TransactionDate)
                })
                .ToListAsync();

            if (!rawData.Any())
                return new List<UserWorkOrderSummaryDto>();

            // 3. Get Work Order Details
            var workOrderIds = rawData.Select(r => r.WorkOrderId).Distinct().ToList();
            var workOrderQuery = _context.WorkOrders.AsNoTracking().Where(w => workOrderIds.Contains(w.Id));

            if (!string.IsNullOrEmpty(buyer))
                workOrderQuery = workOrderQuery.Where(w => w.Buyer.ToLower().Contains(buyer.ToLower()));

            if (!string.IsNullOrEmpty(factory))
                workOrderQuery = workOrderQuery.Where(w => w.Factory.ToLower() == factory.ToLower());

            if (!string.IsNullOrEmpty(unit))
                workOrderQuery = workOrderQuery.Where(w => w.Unit.ToLower() == unit.ToLower());

            var workOrders = await workOrderQuery.ToListAsync();

            // 4. Construct Result
            var result = new List<UserWorkOrderSummaryDto>();

            foreach (var wo in workOrders)
            {
                var woData = rawData.Where(r => r.WorkOrderId == wo.Id).ToList();
                if (!woData.Any()) continue;

                var summary = new UserWorkOrderSummaryDto
                {
                    WorkOrderId = wo.Id,
                    WorkOrderNo = wo.WorkOrderNo ?? "-",
                    StyleName = wo.StyleName ?? "-",
                    Buyer = wo.Buyer ?? "-",
                    Factory = wo.Factory ?? "-",
                    Unit = wo.Unit ?? "-",
                    FastReactNo = wo.FastReactNo,
                    OrderQuantity = wo.OrderQuantity ?? 0,
                    WashTargetDate = wo.WashTargetDate,
                    LastTransactionDate = woData.Max(d => d.LastDate)
                };

                // Helper to get total receive/delivery (USER specific)
                summary.TotalRecieveQuantity = woData.Where(d => d.TransactionType == TransactionType.Receive).Sum(d => d.Quantity);
                summary.TotalDelivaryQuantity = woData.Where(d => d.TransactionType == TransactionType.Delivery).Sum(d => d.Quantity);

                // Global Work Order Totals (from WorkOrder table)
                summary.WorkOrderTotalReceived = wo.TotalWashReceived ?? 0;
                summary.WorkOrderTotalDelivered = wo.TotalWashDelivery ?? 0;

                // Build Stage Quantities List
                summary.StageData = woData
                    .GroupBy(d => new { d.StageName, d.DisplayOrder })
                    .OrderBy(g => g.Key.DisplayOrder)
                    .Select(g => new UserStageSummaryDto
                    {
                        Stage = g.Key.StageName,
                        Recieve = g.Where(d => d.TransactionType == TransactionType.Receive).Sum(d => d.Quantity),
                        Delivary = g.Where(d => d.TransactionType == TransactionType.Delivery).Sum(d => d.Quantity)
                    })
                    .ToList();

                result.Add(summary);
            }

            return result.OrderByDescending(r => r.LastTransactionDate).ToList();
        }

        // ==========================================
        // GET FILTER OPTIONS
        // ==========================================
        public async Task<ReportFilterOptionsDto> GetFilterOptionsAsync()
        {
            try
            {
                var buyers = await _context.WorkOrders
                    .AsNoTracking()
                    .Select(w => w.Buyer)
                    .Where(b => !string.IsNullOrEmpty(b))
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                var factories = await _context.WorkOrders
                    .AsNoTracking()
                    .Select(w => w.Factory)
                    .Where(f => !string.IsNullOrEmpty(f))
                    .Distinct()
                    .OrderBy(f => f)
                    .ToListAsync();

                var units = await _context.WorkOrders
                    .AsNoTracking()
                    .Select(w => w.Unit)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(u => u)
                    .ToListAsync();

                var stages = await _context.ProcessStages
                    .AsNoTracking()
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.DisplayOrder)
                    .Select(s => new ProcessStageOptionDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        DisplayOrder = s.DisplayOrder
                    })
                    .ToListAsync();

                return new ReportFilterOptionsDto
                {
                    Buyers = buyers,
                    Factories = factories,
                    Units = units,
                    ProcessStages = stages
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error getting filter options: {ex.Message}");
                return new ReportFilterOptionsDto();
            }
        }

        // ==========================================
        // PRIVATE: ESCAPE CSV FIELD
        // ==========================================
        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            return field.Replace("\"", "\"\"");
        }

        // ==========================================
        // PRIVATE: APPLY WORK ORDER FILTERS
        // ==========================================
        private IQueryable<WorkOrder> ApplyWorkOrderFilters(
            IQueryable<WorkOrder> query,
            ReportRequestDto request)
        {
            // Search
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var search = request.SearchTerm.ToLower().Trim();
                query = query.Where(w =>
                    w.WorkOrderNo.ToLower().Contains(search) ||
                    w.Buyer.ToLower().Contains(search) ||
                    w.StyleName.ToLower().Contains(search) ||
                    (w.FastReactNo != null && w.FastReactNo.ToLower().Contains(search)) ||
                    w.Factory.ToLower().Contains(search) ||
                    w.Unit.ToLower().Contains(search));
            }

            // Buyer filter
            if (!string.IsNullOrEmpty(request.Buyer))
            {
                query = query.Where(w => w.Buyer.ToLower().Contains(request.Buyer.ToLower()));
            }

            // Factory filter
            if (!string.IsNullOrEmpty(request.Factory))
            {
                query = query.Where(w => w.Factory.ToLower() == request.Factory.ToLower());
            }

            // Unit filter
            if (!string.IsNullOrEmpty(request.Unit))
            {
                query = query.Where(w => w.Unit.ToLower() == request.Unit.ToLower());
            }

            // Wash Target Date filters
            if (request.WashTargetStartDate.HasValue)
            {
                query = query.Where(w => w.WashTargetDate >= request.WashTargetStartDate.Value.Date);
            }

            if (request.WashTargetEndDate.HasValue)
            {
                query = query.Where(w => w.WashTargetDate <= request.WashTargetEndDate.Value.Date);
            }

            return query;
        }

        // ==========================================
        // PRIVATE: APPLY SORTING
        // ==========================================
        private IQueryable<WorkOrder> ApplySorting(
            IQueryable<WorkOrder> query,
            string? sortBy,
            string? sortOrder)
        {
            var isDesc = sortOrder?.ToLower() == "desc";

            return (sortBy?.ToLower()) switch
            {
                "factory" => isDesc ? query.OrderByDescending(w => w.Factory) : query.OrderBy(w => w.Factory),
                "unit" => isDesc ? query.OrderByDescending(w => w.Unit) : query.OrderBy(w => w.Unit),
                "buyer" => isDesc ? query.OrderByDescending(w => w.Buyer) : query.OrderBy(w => w.Buyer),
                "stylename" => isDesc ? query.OrderByDescending(w => w.StyleName) : query.OrderBy(w => w.StyleName),
                "washtargetdate" => isDesc ? query.OrderByDescending(w => w.WashTargetDate) : query.OrderBy(w => w.WashTargetDate),
                "orderquantity" => isDesc ? query.OrderByDescending(w => w.OrderQuantity) : query.OrderBy(w => w.OrderQuantity),
                "totalwashreceived" => isDesc ? query.OrderByDescending(w => w.TotalWashReceived) : query.OrderBy(w => w.TotalWashReceived),
                "totalwashdelivery" => isDesc ? query.OrderByDescending(w => w.TotalWashDelivery) : query.OrderBy(w => w.TotalWashDelivery),
                _ => isDesc ? query.OrderByDescending(w => w.WorkOrderNo) : query.OrderBy(w => w.WorkOrderNo)
            };
        }

        // ==========================================
        // PRIVATE: GET TRANSACTIONS FOR WORK ORDERS
        // ==========================================
        private async Task<Dictionary<int, List<TransactionData>>> GetTransactionsForWorkOrders(
            List<int> workOrderIds,
            DateOnly? startDate,
            DateOnly? endDate,
            int? processStageId,
            int? transactionTypeId,
            int? shiftType)
        {
            if (!workOrderIds.Any())
                return new Dictionary<int, List<TransactionData>>();

            var query = _context.WashTransactions
                .AsNoTracking()
                .Include(t => t.ProcessStage)
                .Where(t => workOrderIds.Contains(t.WorkOrderId) && t.IsActive);

            // ✅ UPDATED: Use ShiftDate for filtering instead of TransactionDate
            if (startDate.HasValue)
                query = query.Where(t => t.ShiftDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(t => t.ShiftDate <= endDate);

            if (processStageId.HasValue)
                query = query.Where(t => t.ProcessStageId == processStageId.Value);

            if (transactionTypeId.HasValue)
            {
                var transactionType = (TransactionType)transactionTypeId.Value;
                query = query.Where(t => t.TransactionType == transactionType);
            }

            if (shiftType.HasValue)
            {
                var shiftTypeEnum = (ShiftType)shiftType.Value;
                query = query.Where(t => t.ShiftType == shiftTypeEnum);
            }

            // ✅ UPDATED: Select shift fields
            var transactions = await query
                .Select(t => new TransactionData
                {
                    WorkOrderId = t.WorkOrderId,
                    ProcessStageId = t.ProcessStageId,
                    StageName = t.ProcessStage.Name,
                    TransactionType = t.TransactionType,
                    Quantity = t.Quantity,
                  //  TransactionDate = t.TransactionDate,

                    // ✅ NEW: Select shift fields
                    ShiftDate = t.ShiftDate,
                    ShiftType = t.ShiftType
                })
                .OrderByDescending(t => t.ShiftDate)

                .ToListAsync();

            return transactions
                .GroupBy(t => t.WorkOrderId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }

        // ==========================================
        // PRIVATE: BUILD REPORT ROWS
        // ==========================================
        //private List<ReportRowDto> BuildReportRows(
        //    List<WorkOrderData> workOrders,
        //    Dictionary<int, List<TransactionData>> transactionsByWorkOrder)
        //{
        //    var reportData = new List<ReportRowDto>();

        //    foreach (var wo in workOrders)
        //    {
        //        var stageQuantities = new Dictionary<string, StageQuantityDto>();

        //        if (transactionsByWorkOrder.TryGetValue(wo.Id, out var woTransactions))
        //        {
        //            var stageGroups = woTransactions.GroupBy(t => t.StageName);
        //            foreach (var stage in stageGroups)
        //            {
        //                stageQuantities[stage.Key] = new StageQuantityDto
        //                {
        //                    Receive = stage.Where(t => t.TransactionType == TransactionType.Receive).Sum(t => t.Quantity),
        //                    Delivery = stage.Where(t => t.TransactionType == TransactionType.Delivery).Sum(t => t.Quantity)
        //                };
        //            }
        //        }

        //        reportData.Add(new ReportRowDto
        //        {
        //            WorkOrderId = wo.Id,
        //            Factory = wo.Factory,
        //            Unit = wo.Unit,
        //            WorkOrderNo = wo.WorkOrderNo,
        //            FastReactNo = wo.FastReactNo ?? "-",
        //            Buyer = wo.Buyer,
        //            StyleName = wo.StyleName,
        //            Marks = wo.Marks,
        //            OrderQuantity = wo.OrderQuantity,
        //            WashTargetDate = wo.WashTargetDate,
        //            TotalWashReceived = wo.TotalWashReceived,
        //            TotalWashDelivery = wo.TotalWashDelivery,
        //            StageQuantities = stageQuantities
        //        });
        //    }

        //    return reportData;
        //}


        private List<ReportRowDto> BuildReportRows(
    List<WorkOrderData> workOrders,
    Dictionary<int, List<TransactionData>> transactionsByWorkOrder)
        {
            var reportData = new List<ReportRowDto>();

            foreach (var wo in workOrders)
            {
                var stageQuantities = new Dictionary<string, StageQuantityDto>();
                var shiftDates = new List<DateOnly>();  // ✅ NEW: Collect all shift dates
                var shiftTypes = new HashSet<string>();  // ✅ NEW: Collect all shift types

                if (transactionsByWorkOrder.TryGetValue(wo.Id, out var woTransactions))
                {
                    var stageGroups = woTransactions.GroupBy(t => t.StageName);
                    foreach (var stage in stageGroups)
                    {
                        stageQuantities[stage.Key] = new StageQuantityDto
                        {
                            Receive = stage.Where(t => t.TransactionType == TransactionType.Receive).Sum(t => t.Quantity),
                            Delivery = stage.Where(t => t.TransactionType == TransactionType.Delivery).Sum(t => t.Quantity)
                        };
                    }

                    // ✅ NEW: Collect shift information from all transactions
                    foreach (var tx in woTransactions)
                    {
                        shiftDates.Add(tx.ShiftDate);
                        shiftTypes.Add(tx.ShiftType.ToString());
                    }
                }

                // ✅ NEW: Determine shift display values
                //var firstShiftDate = shiftDates.Any() ? shiftDates.Min() : DateTime.MinValue;
                //var lastShiftDate = shiftDates.Any() ? shiftDates.Max() : DateTime.MinValue;
                //var shiftTypeDisplay = shiftTypes.Any() ? string.Join(", ", shiftTypes) : "N/A";

                // Determine shift display values
                var firstShiftDate = shiftDates.Any()
                    ? shiftDates.Min()
                    : DateOnly.MinValue;

                var lastShiftDate = shiftDates.Any()
                    ? shiftDates.Max()
                    : DateOnly.MinValue;

                var shiftTypeDisplay = shiftTypes.Any()
                    ? string.Join(", ", shiftTypes)
                    : "N/A";

                reportData.Add(new ReportRowDto
                {
                    WorkOrderId = wo.Id,
                    Factory = wo.Factory,
                    Unit = wo.Unit,
                    WorkOrderNo = wo.WorkOrderNo,
                    FastReactNo = wo.FastReactNo ?? "-",
                    Buyer = wo.Buyer,
                    StyleName = wo.StyleName,
                    Marks = wo.Marks,
                    OrderQuantity = wo.OrderQuantity,
                    WashTargetDate = wo.WashTargetDate,
                    TotalWashReceived = wo.TotalWashReceived,
                    TotalWashDelivery = wo.TotalWashDelivery,
                    StageQuantities = stageQuantities,

                    // ✅ NEW: Shift information
                   // ShiftDate = firstShiftDate != DateTime.MinValue ? firstShiftDate : DateTime.Now,

                    // Shift information
                    ShiftDate = firstShiftDate != DateOnly.MinValue
                ? firstShiftDate
                : DateOnly.FromDateTime(DateTime.Now),
                    ShiftType = shiftTypeDisplay,
                    Status = wo.Status,
                });
            }

            return reportData;
        }
        // ==========================================
        // PRIVATE: CALCULATE SUMMARY
        // ==========================================
        private async Task<ReportSummaryDto> CalculateSummaryAsync(ReportRequestDto request)
        {
            try
            {
                // Get matching work order IDs first
                var workOrderQuery = _context.WorkOrders
                                        .Where(x => x.TotalWashReceived > 0)
                                        .AsNoTracking()
                                        .AsQueryable();
                workOrderQuery = ApplyWorkOrderFilters(workOrderQuery, request);
                if (request.IsCompleted == true)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status == 5);
                }
                else if (request.IsCompleted == false)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status != 5 || w.Status == null);
                }

                // Get total order quantity and count
                var workOrderStats = await workOrderQuery
                    .GroupBy(w => 1)
                    .Select(g => new
                    {
                        TotalCount = g.Count(),
                        TotalOrderQty = g.Sum(w => w.OrderQuantity ?? 0)
                    })
                    .FirstOrDefaultAsync();

                var totalWorkOrders = workOrderStats?.TotalCount ?? 0;
                var totalOrderQuantity = workOrderStats?.TotalOrderQty ?? 0;

                if (totalWorkOrders == 0)
                {
                    return new ReportSummaryDto();
                }

                // Get work order IDs for transaction filtering
                var workOrderIds = await workOrderQuery.Select(w => w.Id).ToListAsync();

                // Build transaction query
                var transactionQuery = _context.WashTransactions
                    .AsNoTracking()
                    .Include(t => t.ProcessStage)
                    .Where(t => t.IsActive && workOrderIds.Contains(t.WorkOrderId));

                // Apply transaction-specific filters
                //if (request.StartDate.HasValue)
                //    transactionQuery = transactionQuery.Where(t => t.ShiftDate.Date >= request.StartDate.Value.Date);

                //if (request.EndDate.HasValue)
                //    transactionQuery = transactionQuery.Where(t => t.ShiftDate.Date <= request.EndDate.Value.Date);

                if (request.StartDate.HasValue)
                {
                    var startDate = request.StartDate.Value.ToDateTime(TimeOnly.MinValue);

                    transactionQuery = transactionQuery
                        .Where(t => t.TransactionDate >= startDate);
                }

                if (request.EndDate.HasValue)
                {
                    var endDate = request.EndDate.Value.ToDateTime(TimeOnly.MaxValue);

                    transactionQuery = transactionQuery
                        .Where(t => t.TransactionDate <= endDate);
                }
                if (request.ProcessStageId.HasValue)
                    transactionQuery = transactionQuery.Where(t => t.ProcessStageId == request.ProcessStageId.Value);

                if (request.TransactionTypeId.HasValue)
                {
                    var transactionType = (TransactionType)request.TransactionTypeId.Value;
                    transactionQuery = transactionQuery.Where(t => t.TransactionType == transactionType);
                }

                // Get aggregated data
                var summaryData = await transactionQuery
                    .GroupBy(t => new { t.ProcessStage.Name, t.TransactionType })
                    .Select(g => new
                    {
                        StageName = g.Key.Name,
                        TransactionType = g.Key.TransactionType,
                        TotalQty = g.Sum(t => t.Quantity),
                        Count = g.Count()
                    })
                    .ToListAsync();

                var stageBreakdown = new Dictionary<string, StageQuantityDto>();
                decimal totalReceive = 0, totalDelivery = 0, totalCount = 0;

                foreach (var item in summaryData)
                {
                    if (!stageBreakdown.ContainsKey(item.StageName))
                        stageBreakdown[item.StageName] = new StageQuantityDto();

                    if (item.TransactionType == TransactionType.Receive) // Receive
                    {
                        stageBreakdown[item.StageName].Receive = item.TotalQty;
                        totalReceive += item.TotalQty;
                    }
                    else // Delivery
                    {
                        stageBreakdown[item.StageName].Delivery = item.TotalQty;
                        totalDelivery += item.TotalQty;
                    }
                    totalCount += item.Count;
                }

                return new ReportSummaryDto
                {
                    TotalWorkOrders = totalWorkOrders,
                    TotalTransactions = totalCount,
                    TotalReceiveQty = totalReceive,
                    TotalDeliveryQty = totalDelivery,
                    TotalOrderQuantity = totalOrderQuantity,
                    StageBreakdown = stageBreakdown
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating summary: {ex.Message}");
                return new ReportSummaryDto();
            }
        }

        public async Task<byte[]> ExportShortCsvAsync(ReportRequestDto request)
        {
            try
            {
                Console.WriteLine("📥 ExportShortCsvAsync called");

                // ==========================================
                // SOURCE 1: Get local data - DATE-WISE (1st Dry, 1st Wash, 2nd Dry)
                // Key: (WorkOrderNo, ShiftDate)
                // ==========================================
                var workOrderQuery = _context.WorkOrders.AsNoTracking().AsQueryable();
                workOrderQuery = ApplyWorkOrderFilters(workOrderQuery, request);
                if (request.IsCompleted == true)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status == 5);
                }
                else if (request.IsCompleted == false)
                {
                    workOrderQuery = workOrderQuery.Where(w => w.Status != 5 || w.Status == null);
                }

                if (request.StartDate.HasValue || request.EndDate.HasValue ||
                    request.ProcessStageId.HasValue || request.TransactionTypeId.HasValue)
                {
                    var transactionQuery = _context.WashTransactions
                        .AsNoTracking()
                        .Where(t => t.IsActive);

                    if (request.StartDate.HasValue)
                    {
                        var startDate = request.StartDate;
                        transactionQuery = transactionQuery.Where(t => t.ShiftDate >= startDate);
                    }

                    if (request.EndDate.HasValue)
                    {
                        var endDate = request.EndDate;
                        transactionQuery = transactionQuery.Where(t => t.ShiftDate <= endDate);
                    }

                    if (request.ProcessStageId.HasValue)
                        transactionQuery = transactionQuery.Where(t => t.ProcessStageId == request.ProcessStageId.Value);

                    if (request.TransactionTypeId.HasValue)
                    {
                        var transactionType = (TransactionType)request.TransactionTypeId.Value;
                        transactionQuery = transactionQuery.Where(t => t.TransactionType == transactionType);
                    }

                    var matchingWorkOrderIds = await transactionQuery
                        .Select(t => t.WorkOrderId)
                        .Distinct()
                        .ToListAsync();

                    workOrderQuery = workOrderQuery.Where(w => matchingWorkOrderIds.Contains(w.Id));
                }

                var workOrders = await workOrderQuery
                    .Select(w => new WorkOrderData
                    {
                        Id = w.Id,
                        Factory = w.Factory ?? "",
                        Unit = w.Unit ?? "",
                        WorkOrderNo = w.WorkOrderNo ?? "",
                        FastReactNo = w.FastReactNo,
                        Buyer = w.Buyer ?? "",
                        StyleName = w.StyleName ?? "",
                        Marks = w.Marks,
                        OrderQuantity = w.OrderQuantity ?? 0,
                        WashTargetDate = w.WashTargetDate,
                        TotalWashReceived = w.TotalWashReceived ?? 0,
                        TotalWashDelivery = w.TotalWashDelivery ?? 0,
                        Status = (int?)w.Status
                    })
                    .ToListAsync();

                var workOrderIds = workOrders.Select(w => w.Id).ToList();

                // Build WorkOrderId -> WorkOrderNo + FastReactNo lookup
                var workOrderMetaById = workOrders
                    .Where(w => !string.IsNullOrEmpty(w.WorkOrderNo))
                    .ToDictionary(w => w.Id, w => new { w.WorkOrderNo, w.FastReactNo });

                // Get ALL transactions DATE-WISE grouped by (WorkOrderId, ShiftDate, Stage)
                // IMPORTANT: Apply StartDate/EndDate filter to transactions to avoid returning dates outside the range
                var rawTxQuery = _context.WashTransactions
                    .AsNoTracking()
                    .Include(t => t.ProcessStage)
                    .Where(t => workOrderIds.Contains(t.WorkOrderId) && t.IsActive);

                if (request.StartDate.HasValue)
                {
                    rawTxQuery = rawTxQuery.Where(t => t.ShiftDate >= request.StartDate.Value);
                }

                if (request.EndDate.HasValue)
                {
                    rawTxQuery = rawTxQuery.Where(t => t.ShiftDate <= request.EndDate.Value);
                }

                var rawTransactions = await rawTxQuery
                    .Select(t => new
                    {
                        t.WorkOrderId,
                        t.ShiftDate,
                        StageName = t.ProcessStage.Name,
                        t.TransactionType,
                        t.Quantity
                    })
                    .ToListAsync();

                // Aggregate DATE-WISE local data
                // Key: WorkOrderNo + "|" + yyyy-MM-dd
                var localByWorkOrderDate = new Dictionary<string, (decimal FirstDry, decimal FirstWash, decimal SecondDry)>(StringComparer.OrdinalIgnoreCase);
                var localFastReactByWorkOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                foreach (var wo in workOrders)
                {
                    if (string.IsNullOrEmpty(wo.WorkOrderNo)) continue;
                    if (!string.IsNullOrEmpty(wo.FastReactNo))
                    {
                        localFastReactByWorkOrder[wo.WorkOrderNo] = wo.FastReactNo;
                    }
                }

                foreach (var tx in rawTransactions)
                {
                    if (!workOrderMetaById.TryGetValue(tx.WorkOrderId, out var meta)) continue;

                    // Skip transactions outside date range (double-check)
                    if (request.StartDate.HasValue && tx.ShiftDate < request.StartDate.Value) continue;
                    if (request.EndDate.HasValue && tx.ShiftDate > request.EndDate.Value) continue;

                    var key = $"{meta.WorkOrderNo}|{tx.ShiftDate:yyyy-MM-dd}";
                    if (!localByWorkOrderDate.ContainsKey(key))
                    {
                        localByWorkOrderDate[key] = (0, 0, 0);
                    }

                    var current = localByWorkOrderDate[key];

                    if (tx.StageName == "1st Dry" && tx.TransactionType == TransactionType.Delivery)
                    {
                        current.FirstDry += tx.Quantity;
                    }
                    else if (tx.StageName == "1st Wash" && tx.TransactionType == TransactionType.Delivery)
                    {
                        current.FirstWash += tx.Quantity;
                    }
                    else if (tx.StageName == "2nd Dry" && tx.TransactionType == TransactionType.Delivery)
                    {
                        current.SecondDry += tx.Quantity;
                    }

                    localByWorkOrderDate[key] = current;
                }

                // ==========================================
                // SOURCE 2: Get Final Wash from TusukaExtreme - DATE-WISE
                // Fetch ALL rows in a single call (no pagination loop)
                // ==========================================
                List<string>? plantFilter = null;
                if (!string.IsNullOrEmpty(request.Factory))
                {
                    plantFilter = new List<string> { request.Factory };
                }

                List<string>? unitFilter = null;
                if (!string.IsNullOrEmpty(request.Unit))
                {
                    unitFilter = new List<string> { request.Unit };
                }

                var washDeliveryData = await _tusukaExtremeRepo.GetWashDeliveryDetailsAsync(
                    request.StartDate,
                    request.EndDate,
                    plantFilter,
                    unitFilter,
                    1,
                    int.MaxValue
                );

                // Key: WorkOrderNo + "|" + yyyy-MM-dd
                var finalWashByWorkOrderDate = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
                var tusukaFastReactByWorkOrder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var productionDateByKey = new Dictionary<string, DateOnly>(StringComparer.OrdinalIgnoreCase);

                foreach (var wd in washDeliveryData.Data)
                {
                    if (string.IsNullOrEmpty(wd.WorkOrderNo)) continue;

                    if (!string.IsNullOrEmpty(wd.FastReactNo))
                    {
                        tusukaFastReactByWorkOrder[wd.WorkOrderNo] = wd.FastReactNo;
                    }

                    DateOnly prodDate;
                    if (wd.ProductionDate.HasValue)
                    {
                        prodDate = DateOnly.FromDateTime(wd.ProductionDate.Value);
                    }
                    else
                    {
                        continue;
                    }

                    // Skip rows outside the requested date range (double-check)
                    if (request.StartDate.HasValue && prodDate < request.StartDate.Value) continue;
                    if (request.EndDate.HasValue && prodDate > request.EndDate.Value) continue;

                    var key = $"{wd.WorkOrderNo}|{prodDate:yyyy-MM-dd}";

                    if (!productionDateByKey.ContainsKey(key))
                    {
                        productionDateByKey[key] = prodDate;
                    }

                    if (!finalWashByWorkOrderDate.ContainsKey(key))
                    {
                        finalWashByWorkOrderDate[key] = 0;
                    }

                    finalWashByWorkOrderDate[key] += wd.Delivery;
                }

                // ==========================================
                // FULL OUTER JOIN: Merge (WorkOrderNo, Date) from BOTH sources
                // ==========================================
                var allKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var key in localByWorkOrderDate.Keys)
                    allKeys.Add(key);
                foreach (var key in finalWashByWorkOrderDate.Keys)
                    allKeys.Add(key);

                if (allKeys.Count == 0)
                {
                    throw new Exception("No data to export");
                }

                // Build CSV
                var sb = new StringBuilder();
                var preamble = Encoding.UTF8.GetPreamble();

                // Headers: Work Order No, FastReact No, Production Date, 1st Dry, 1st Wash, 2nd Dry, Final Wash
                sb.AppendLine("Work Order No,FastReact No,Production Date,1st Dry,1st Wash,2nd Dry,Final Wash");

                // Sort by Date (ascending) first, then by WorkOrderNo (ascending)
                var sortedKeys = allKeys
                    .Select(key =>
                    {
                        var parts = key.Split('|');
                        var workOrderNo = parts[0];
                        var dateStr = parts.Length > 1 ? parts[1] : "";

                        DateOnly? productionDate = null;
                        if (productionDateByKey.TryGetValue(key, out var pDate))
                        {
                            productionDate = pDate;
                        }
                        else if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", out var parsed))
                        {
                            productionDate = parsed;
                        }

                        return new { Key = key, WorkOrderNo = workOrderNo, ProductionDate = productionDate };
                    })
                    .OrderBy(x => x.ProductionDate.HasValue ? 0 : 1)
                    .ThenBy(x => x.ProductionDate)
                    .ThenBy(x => x.WorkOrderNo, StringComparer.OrdinalIgnoreCase)
                    .Select(x => x.Key)
                    .ToList();

                foreach (var key in sortedKeys)
                {
                    var parts = key.Split('|');
                    var workOrderNo = parts[0];
                    var dateStr = parts.Length > 1 ? parts[1] : "";

                    // Parse date
                    DateOnly? productionDate = null;
                    if (productionDateByKey.TryGetValue(key, out var pDate))
                    {
                        productionDate = pDate;
                    }
                    else if (DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", out var parsed))
                    {
                        productionDate = parsed;
                    }

                    // Get local date-wise values
                    var firstDry = 0m;
                    var firstWash = 0m;
                    var secondDry = 0m;
                    if (localByWorkOrderDate.TryGetValue(key, out var localDel))
                    {
                        firstDry = localDel.FirstDry;
                        firstWash = localDel.FirstWash;
                        secondDry = localDel.SecondDry;
                    }

                    // Get Final Wash date-wise
                    finalWashByWorkOrderDate.TryGetValue(key, out var finalWash);

                    // Get FastReactNo (prefer local, fallback to TusukaExtreme)
                    string? fastReactNo = null;
                    if (!localFastReactByWorkOrder.TryGetValue(workOrderNo, out fastReactNo))
                    {
                        tusukaFastReactByWorkOrder.TryGetValue(workOrderNo, out fastReactNo);
                    }

                    var values = new List<string>
                    {
                        $"=\"{EscapeCsvField(workOrderNo)}\"",
                        $"=\"{EscapeCsvField(fastReactNo ?? "")}\"",
                        productionDate.HasValue ? productionDate.Value.ToString("yyyy-MM-dd") : "",
                        firstDry.ToString(),
                        firstWash.ToString(),
                        secondDry.ToString(),
                        finalWash.ToString()
                    };

                    sb.AppendLine(string.Join(",", values));
                }

                Console.WriteLine($"✅ Short CSV export completed - {allKeys.Count} records");

                var content = Encoding.UTF8.GetBytes(sb.ToString());
                var result = new byte[preamble.Length + content.Length];
                preamble.CopyTo(result, 0);
                content.CopyTo(result, preamble.Length);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Short CSV export error: {ex.Message}");
                Console.WriteLine($"   Stack: {ex.StackTrace}");
                throw;
            }
        }

    }
}