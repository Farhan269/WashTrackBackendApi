using wsahRecieveDelivary.Common.Models;
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.IRepository
{
    public interface IWashDhuRepository
    {
        Task<IEnumerable<DhuDryProcessSummaryDTO>>GetDryProcessSummaryAsync(DryProcessSummaryFilterDto filter);
        Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(DryProcessSummaryFilterDto filter);

        Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(DryProcessSummaryFilterDto filter);

        Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(DryProcessSummaryFilterDto filter);

        //Task<IEnumerable<DhuDryProcessDetailsDTO>> GetDryProcessDetailsAsync(DryProcessDetailsFilterDto filter);
        Task<PagedResult<DhuDryProcessDetailsDTO>> GetDryProcessDetailsAsync(DryProcessDetailsFilterDto filter);
        Task<PagedResult<DhuDryProcessDetailsDTO>> GetWetProcessDetailsAsync(DryProcessDetailsFilterDto filter);

        Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetDryProcessHourlyDetailsAsync(DhuDryProcessHourlyDetailsFilterDto filter);
        Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetWetProcessHourlyDetailsAsync(DhuDryProcessHourlyDetailsFilterDto filter);
    }
}
