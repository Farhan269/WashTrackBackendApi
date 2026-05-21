using Microsoft.AspNetCore.Http;
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IWorkOrderService
    {
        // CRUD Operations
        Task<WorkOrderResponseDto> CreateAsync(WorkOrderDto dto, int userId);
        Task<WorkOrderResponseDto> UpdateAsync(int id, WorkOrderDto dto, int userId);
        Task<bool> DeleteAsync(int id);

        // Read Operations
        Task<WorkOrderResponseDto?> GetByIdAsync(int id);
        Task<WorkOrderResponseDto?> GetByWorkOrderNoAsync(string workOrderNo);
        Task<List<WorkOrderResponseDto>> GetAllAsync();

        // Pagination
        Task<PaginatedResponseDto<WorkOrderResponseDto>> GetPaginatedAsync(PaginationRequestDto request);

        // Bulk Upload
        Task<WorkOrderBulkUploadResponseDto> BulkUploadFromExcelAsync(IFormFile file, int userId);
    }
}