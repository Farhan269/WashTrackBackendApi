using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.IRepository;

namespace wsahRecieveDelivary.WashService
{
    public class WashDhuService
    {
        private readonly IWashDhuRepository _repository;
        public WashDhuService(IWashDhuRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>>GetDryProcessSummaryAsync(DryProcessSummaryFilterDto filter)
        {
            return await _repository
                .GetDryProcessSummaryAsync(filter);
        }

        public async Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        {
            return await _repository.GetTopIssuesAsync(filter);
        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(DryProcessSummaryFilterDto filter)
        {
            return await _repository
                .GetWetProcessSummaryAsync(filter);
        }
        public async Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        {
            return await _repository.GetWetTopIssuesAsync(filter);
        }
    }
}
