using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.IRepository
{
    public interface IWashDhuRepository
    {
        Task<IEnumerable<DhuDryProcessSummaryDTO>>GetDryProcessSummaryAsync(DryProcessSummaryFilterDto filter);
        Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(DryProcessSummaryFilterDto filter);

        Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(DryProcessSummaryFilterDto filter);

        Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(DryProcessSummaryFilterDto filter);

    }
}
