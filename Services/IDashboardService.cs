using System.Security.Claims;
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IDashboardService
    {
        Task<List<DashboardDto>> GetDashboardDataAsync(DateOnly? fromDate, DateOnly? toDate,string? plant,string? unit,int? shift, ClaimsPrincipal? user);

        Task<DashboardDetailsResponseDto> GetDashboardDetailsAsync(DateOnly? fromDate, DateOnly? toDate,string? plant, string? unit,int? shift, List<int>? processStageIds,string? search,int page,int pageSize, ClaimsPrincipal? user);

        Task<List<PlantUnitDto>> GetPlantUnitListAsync();
        Task<List<GetWashMachineDto>> GetMachineListAsync(int? plantId = null, int? unitId = null);
    }
}
