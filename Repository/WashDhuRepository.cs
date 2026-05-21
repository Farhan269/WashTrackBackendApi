using Dapper;
using Microsoft.Data.SqlClient;
using wsahRecieveDelivary.Dapper;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.Queries;

namespace wsahRecieveDelivary.Repository
{
    public class WashDhuRepository : IWashDhuRepository
    {
        private readonly WashDhuContext _context;

        public WashDhuRepository(WashDhuContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetDryProcessSummaryAsync(
DryProcessSummaryFilterDto filter)
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantIds = filter.PlantId != null && filter.PlantId.Any()
                    ? string.Join(",", filter.PlantId)
                    : null,

                UnitIds = filter.UnitId != null && filter.UnitId.Any()
                    ? string.Join(",", filter.UnitId)
                    : null,

                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
                    ? string.Join(",", filter.ProcessModuleId)
                    : null,

                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
                    ? string.Join(",", filter.WashProcessId)
                    : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                    ? string.Join(",", filter.Shift)
                    : null
            };

            var result = await connection.QueryAsync<DhuDryProcessSummaryDTO>(
                DryProcessSummaryQuery.GetSummary,
                parameters
            );

            return result;
        }


        public async Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        {
            using var connection =
                _context.CreateConnection();

            var parameters = new
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantIds = filter.PlantId != null && filter.PlantId.Any()
                   ? string.Join(",", filter.PlantId)
                   : null,

                UnitIds = filter.UnitId != null && filter.UnitId.Any()
                   ? string.Join(",", filter.UnitId)
                   : null,

                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
                   ? string.Join(",", filter.ProcessModuleId)
                   : null,

                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
                   ? string.Join(",", filter.WashProcessId)
                   : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                   ? string.Join(",", filter.Shift)
                   : null
            };

            var result = await connection.QueryAsync<TopIssueDTO>(DryProcessSummaryQuery.GetTopIssues,parameters
            );

            return result;
        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(
DryProcessSummaryFilterDto filter)
        {
            using var connection = _context.CreateConnection();

            var parameters = new
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantIds = filter.PlantId != null && filter.PlantId.Any()
                    ? string.Join(",", filter.PlantId)
                    : null,

                UnitIds = filter.UnitId != null && filter.UnitId.Any()
                    ? string.Join(",", filter.UnitId)
                    : null,

                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
                    ? string.Join(",", filter.ProcessModuleId)
                    : null,

                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
                    ? string.Join(",", filter.WashProcessId)
                    : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                    ? string.Join(",", filter.Shift)
                    : null
            };

            var result = await connection.QueryAsync<DhuDryProcessSummaryDTO>(
                DryProcessSummaryQuery.GetWetSummary,
                parameters
            );

            return result;
        }

        public async Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        {
            using var connection =
                _context.CreateConnection();

            var parameters = new
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantIds = filter.PlantId != null && filter.PlantId.Any()
                   ? string.Join(",", filter.PlantId)
                   : null,

                UnitIds = filter.UnitId != null && filter.UnitId.Any()
                   ? string.Join(",", filter.UnitId)
                   : null,

                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
                   ? string.Join(",", filter.ProcessModuleId)
                   : null,

                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
                   ? string.Join(",", filter.WashProcessId)
                   : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                   ? string.Join(",", filter.Shift)
                   : null
            };

            var result = await connection.QueryAsync<TopIssueDTO>(DryProcessSummaryQuery.GetWetTopIssues, parameters
            );

            return result;
        }
    }
}
