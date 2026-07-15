using wsahRecieveDelivary.Common.Models;
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

        //public async Task<IEnumerable<DhuDryProcessDetailsDTO>> GetDryProcessDetailsAsync(DryProcessDetailsFilterDto filter)
        //{
        //    return await _repository
        //        .GetDryProcessDetailsAsync(filter);
        //}

        public async  Task<PagedResult<DhuDryProcessDetailsDTO>>GetDryProcessDetailsAsync( DryProcessDetailsFilterDto filter)
        {
            return await _repository
                .GetDryProcessDetailsAsync(filter);
        }

        public async Task<PagedResult<DhuDryProcessDetailsDTO>> GetWetProcessDetailsAsync(DryProcessDetailsFilterDto filter)
        {
            return await _repository
                .GetWetProcessDetailsAsync(filter);
        }

        public async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetDryProcessHourlyDetailsAsync(DhuDryProcessHourlyDetailsFilterDto filter)
        {
            return await _repository
                .GetDryProcessHourlyDetailsAsync(filter);
        }

        public async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetWetProcessHourlyDetailsAsync(DhuDryProcessHourlyDetailsFilterDto filter)
        {
            return await _repository
                .GetWetProcessHourlyDetailsAsync(filter);
        }
    }
}
