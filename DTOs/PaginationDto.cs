using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace wsahRecieveDelivary.DTOs
{
    // ==========================================
    // PAGINATION REQUEST
    // ==========================================
    public class PaginationRequestDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>
        /// Search across all text fields (WorkOrderNo, Buyer, Style, Color, etc.)
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Sort by: CreatedAt, WorkOrderNo, Buyer, StyleName, WashType, WashTargetDate, UpdatedAt
        /// </summary>
        public string? SortBy { get; set; } = "CreatedAt";

        /// <summary>
        /// Sort order: asc or desc
        /// </summary>
        public string SortOrder { get; set; } = "desc";

        // Advanced Filters
        public string? Factory { get; set; }
        public string? Buyer { get; set; }
        public string? WashType { get; set; }
        public string? Line { get; set; }
        public string? Unit { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    // ==========================================
    // PAGINATED RESPONSE
    // ==========================================
    public class PaginatedResponseDto<T>
    {
        public bool Success { get; set; } = true;
        public string? Message { get; set; }
        public List<T> Data { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }

    // ==========================================
    // PAGINATION METADATA
    // ==========================================
    public class PaginationMetadata
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }
        public int? PreviousPage => HasPrevious ? CurrentPage - 1 : null;
        public int? NextPage => HasNext ? CurrentPage + 1 : null;
        public int FirstPage => 1;
        public int LastPage => TotalPages;
    }


    // ==========================================
    // TRANSACTION PAGINATION REQUEST
    // ==========================================
    // ==========================================
    // TRANSACTION PAGINATION REQUEST (UPDATED)
    // ==========================================
    public class TransactionPaginationRequestDto
    {
        private const int MaxPageSize = 100;
        private int _pageSize = 10;

        [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
        public int Page { get; set; } = 1;

        [Range(1, MaxPageSize, ErrorMessage = "PageSize must be between 1 and 100")]
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
        }

        public string? SearchTerm { get; set; }
        public string? SortBy { get; set; } = "TransactionDate";
        public string SortOrder { get; set; } = "desc";

        // Advanced Filters
        public string? Buyer { get; set; }
        public string? Factory { get; set; }
        public string? Unit { get; set; }
        public int? ProcessStageId { get; set; }
        public int? TransactionTypeId { get; set; }

        // ✅ NEW: Optional date range for summary reports
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // ✅ NEW: Flag to include day-wise breakdown
        [DefaultValue(false)]
        public bool IncludeDayWiseBreakdown { get; set; } = false;
    }
}