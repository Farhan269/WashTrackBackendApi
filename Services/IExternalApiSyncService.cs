using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IExternalApiSyncService
    {
        Task<SyncResultDto> SyncWorkOrdersAsync();
        Task<SyncResultDto> AutoSyncWorkOrdersAsync();
        Task<SyncStatusDto> GetLastSyncStatusAsync();
    }
}