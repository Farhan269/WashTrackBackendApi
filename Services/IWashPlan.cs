using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IWashPlan
    {
        // Task<List<GetWashMachineDto>> GetMachineListAsync(int? plantId = null, int? unitId = null);

        Task<MessageHelper> CreateWashPlanAsync(List<CreateWashPlanDto> data);
        Task<MessageHelper> DeleteWashPlanAsync(long washPlanId,int? UpdatedBy);

        // Task<List<PlantUnitDto>> GetPlantUnitListAsync();
        Task<ApiResponse<object>> GetWashPlanAsync(WashPlanFilterDto filter);

        Task<ApiResponse<object>> GetWashPlanModalAsync(WashPlanModalFilterDto filter);
    }
}
