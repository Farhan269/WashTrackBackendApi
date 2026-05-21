using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IOutServiceApi
    {
        
       Task<List<DryProcessSummaryDTO>> GetDryProcessSummaryAsync(DateOnly? fromDate,DateOnly? toDate);
        
    }
}
