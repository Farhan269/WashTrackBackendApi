// D:\test c#\wsahRecieveDelivary\Services\IReportService.cs
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IReportService
    {
        
        /// <summary>
        /// Get transaction report with pagination, filters, and pre-calculated summary
        /// </summary>
        Task<ReportResponseDto> GetTransactionReportAsync(ReportRequestDto request);

        /// <summary>
        /// Get summary statistics only (for dashboard)
        /// </summary>
        Task<ReportSummaryDto> GetSummaryAsync(ReportRequestDto request);

        /// <summary>
        /// Export report to CSV (server-side generation)
        /// </summary>
        Task<byte[]> ExportToCsvAsync(ReportRequestDto request);

        /// <summary>
        /// Get filter options for dropdowns
        /// </summary>
        Task<ReportFilterOptionsDto> GetFilterOptionsAsync();

        /// <summary>
        /// Get transaction history for a specific user
        /// </summary>
        Task<List<UserTransactionHistoryDto>> GetUserTransactionHistoryAsync(int userId, DateTime? startDate, DateTime? endDate);

        /// <summary>
        /// Get work order summary for a specific user
        /// </summary>
        Task<List<UserWorkOrderSummaryDto>> GetUserWorkOrderSummaryAsync(
            int userId, 
            DateTime? startDate = null, 
            DateTime? endDate = null,
            string? buyer = null,
            string? factory = null,
            string? unit = null,
            int? processStageId = null);

        
    }
}