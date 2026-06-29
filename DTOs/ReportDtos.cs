// D:\test c#\wsahRecieveDelivary\DTOs\ReportDtos.cs
using System.ComponentModel.DataAnnotations;

namespace wsahRecieveDelivary.DTOs
{
    // ==========================================
    // REPORT REQUEST DTO (Input)
    // ==========================================
    public class ReportRequestDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 25;

        // Pagination
        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        // Search
        public string? SearchTerm { get; set; }

        // Filters
        public string? Buyer { get; set; }
        public string? Factory { get; set; }
        public string? Unit { get; set; }
        public int? ProcessStageId { get; set; }
        public int? TransactionTypeId { get; set; }

        // Transaction Date Range
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }

        // Wash Target Date Range
        public DateTime? WashTargetStartDate { get; set; }
        public DateTime? WashTargetEndDate { get; set; }

        // Sorting
        public string SortBy { get; set; } = "workOrderNo";
        public string SortOrder { get; set; } = "asc";

        public int? ShiftType { get; set; }
        public bool? IsCompleted { get; set; }
    }

    // ==========================================
    // REPORT RESPONSE DTO (Output)
    // ==========================================
    public class ReportResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        // Paginated data
        public List<ReportRowDto> Data { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();

        // Pre-calculated summary (for current filter)
        public ReportSummaryDto Summary { get; set; } = new();

        // Filter options (for dropdowns)
        public ReportFilterOptionsDto FilterOptions { get; set; } = new();
    }

    // ==========================================
    // REPORT ROW DTO (Each row in table)
    // ==========================================
    public class ReportRowDto
    {
        public int WorkOrderId { get; set; }
        public string Factory { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string WorkOrderNo { get; set; } = string.Empty;
        public string FastReactNo { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;
        public string StyleName { get; set; } = string.Empty;
        public string? Marks { get; set; }
        public int OrderQuantity { get; set; }
        public DateTime? WashTargetDate { get; set; }

        // Total wash (from WorkOrder table)
        public int TotalWashReceived { get; set; }
        public int TotalWashDelivery { get; set; }

        // Stage-wise quantities (pre-calculated by server)
        // Key = Stage Name, Value = Receive/Delivery quantities
        public Dictionary<string, StageQuantityDto> StageQuantities { get; set; } = new();
        // ✅ NEW: Shift-related fields
        public DateOnly ShiftDate { get; set; }
        public string ShiftType { get; set; } = string.Empty;  // "Day" or "Night"
       // public bool? IsCompleted { get; set; }
        public int? Status { get; set; }

    }

    // ==========================================
    // STAGE QUANTITY DTO
    // ==========================================
    public class StageQuantityDto
    {
        public decimal Receive { get; set; }
        public decimal Delivery { get; set; }
        public decimal Balance => Receive - Delivery;
    }

    // ==========================================
    // REPORT SUMMARY DTO
    // ==========================================
    public class ReportSummaryDto
    {
        public int TotalWorkOrders { get; set; }
        public decimal TotalTransactions { get; set; }
        public decimal TotalReceiveQty { get; set; }
        public decimal TotalDeliveryQty { get; set; }
        public decimal TotalOrderQuantity { get; set; }
        public decimal Balance => TotalReceiveQty - TotalDeliveryQty;

        // Stage breakdown
        public Dictionary<string, StageQuantityDto> StageBreakdown { get; set; } = new();
    }

    // ==========================================
    // FILTER OPTIONS DTO (For dropdowns)
    // ==========================================
    public class ReportFilterOptionsDto
    {
        public List<string> Buyers { get; set; } = new();
        public List<string> Factories { get; set; } = new();
        public List<string> Units { get; set; } = new();
        public List<ProcessStageOptionDto> ProcessStages { get; set; } = new();
    }

    // ==========================================
    // PROCESS STAGE OPTION DTO
    // ==========================================
    public class ProcessStageOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }

    // ==========================================
    // USER TRANSACTION HISTORY DTO
    // ==========================================
    public class UserTransactionHistoryDto
    {
        public int TransactionId { get; set; }
        public int WorkOrderId { get; set; }
        public string WorkOrderNo { get; set; } = string.Empty;
        public string StyleName { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;       // Added
        public string Factory { get; set; } = string.Empty;     // Added
        public string Unit { get; set; } = string.Empty;        // Added
        public string? FastReactNo { get; set; }
        public DateTime? WashTargetDate { get; set; }
        public string StageName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // "Receive" or "Delivery"
        public decimal Quantity { get; set; }
        public DateTime TransactionDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ==========================================
    // USER WORK ORDER SUMMARY DTO
    // ==========================================
    public class UserWorkOrderSummaryDto
    {
        public int WorkOrderId { get; set; }
        public string WorkOrderNo { get; set; } = string.Empty;
        public string StyleName { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;
        public string Factory { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string? FastReactNo { get; set; }
        public decimal OrderQuantity { get; set; }
        public decimal TotalRecieveQuantity { get; set; }
        public decimal TotalDelivaryQuantity { get; set; }
        public decimal Balance => TotalRecieveQuantity - TotalDelivaryQuantity;
        
        // Global Work Order Totals
        public int WorkOrderTotalReceived { get; set; }
        public int WorkOrderTotalDelivered { get; set; }

        public DateTime? WashTargetDate { get; set; }
        public DateTime LastTransactionDate { get; set; }
        
        // Stage breakdown
        public List<UserStageSummaryDto> StageData { get; set; } = new();
    }

    public class UserStageSummaryDto
    {
        public string Stage { get; set; } = string.Empty;
        public decimal Recieve { get; set; }
        public decimal Delivary { get; set; }
    }
}