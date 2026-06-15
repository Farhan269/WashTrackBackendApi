using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using wsahRecieveDelivary.Dapper;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.IRepository;
using wsahRecieveDelivary.Queries;

namespace wsahRecieveDelivary.Repository
{
    public class WashDhuRepository : IWashDhuRepository
    {
        private readonly WashDhuContext _context;
        private readonly WashDhuTWLContext _twlContext;

        private const int TplUnitId = 1;
        private const int TwlUnitId = 2;

        public WashDhuRepository(WashDhuContext context, WashDhuTWLContext twlContext)
        {
            _context = context;
            _twlContext = twlContext;
        }

        //        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetDryProcessSummaryAsync(
        //DryProcessSummaryFilterDto filter)
        //        {
        //            using var connection = _context.CreateConnection();


        //            var parameters = new
        //            {
        //                FromDate = filter.FromDate,
        //                ToDate = filter.ToDate,

        //                PlantIds = filter.PlantId != null && filter.PlantId.Any()
        //                    ? string.Join(",", filter.PlantId)
        //                    : null,

        //                UnitIds = filter.UnitId != null && filter.UnitId.Any()
        //                    ? string.Join(",", filter.UnitId)
        //                    : null,

        //                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
        //                    ? string.Join(",", filter.ProcessModuleId)
        //                    : null,

        //                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
        //                    ? string.Join(",", filter.WashProcessId)
        //                    : null,

        //                ShiftList = filter.Shift != null && filter.Shift.Any()
        //                    ? string.Join(",", filter.Shift)
        //                    : null
        //            };

        //            var result = await connection.QueryAsync<DhuDryProcessSummaryDTO>(
        //                DryProcessSummaryQuery.GetSummary,
        //                parameters, commandTimeout: 300


        //            );

        //            return result;
        //        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetDryProcessSummaryAsync(
           DryProcessSummaryFilterDto filter)
        {
            var selectedUnits = filter.UnitId ?? new List<int>();

            bool hasUnitFilter = selectedUnits.Any();

            bool needTpl = !hasUnitFilter || selectedUnits.Contains(TplUnitId);
            bool needTwl = !hasUnitFilter || selectedUnits.Contains(TwlUnitId);

            var allData = new List<DhuDryProcessSummaryDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TplUnitId).ToList()
                        : null
                );

                var tplData = await QueryDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TwlUnitId).ToList()
                        : null
                );

                var twlData = await QueryDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateSummary(allData);
        }

        private async Task<IEnumerable<DhuDryProcessSummaryDTO>> QueryDatabaseAsync(
            IDbConnection connection,
            DryProcessSummaryFilterDto filter)
        {
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

            return await connection.QueryAsync<DhuDryProcessSummaryDTO>(
                DryProcessSummaryQuery.GetSummary,
                parameters,
                commandTimeout: 300
            );
        }

        private DryProcessSummaryFilterDto CloneFilter(
            DryProcessSummaryFilterDto filter,
            List<int>? unitIds)
        {
            return new DryProcessSummaryFilterDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,
                PlantId = filter.PlantId,
                UnitId = unitIds,
                ProcessModuleId = filter.ProcessModuleId,
                WashProcessId = filter.WashProcessId,
                Shift = filter.Shift
            };
        }

        private IEnumerable<DhuDryProcessSummaryDTO> AggregateSummary(
            IEnumerable<DhuDryProcessSummaryDTO> data)
        {
            return data
                .GroupBy(x => new
                {
                    x.ProcessModuleId,
                    x.ProcessModuleName,
                    x.WashProcessId,
                    x.ProcessName
                })
                .Select(g =>
                {
                    var passQty = g.Sum(x => x.PassQty);
                    var defectQty = g.Sum(x => x.DefectQty);
                    var rejectQty = g.Sum(x => x.RejectQty);
                    var issueQty = g.Sum(x => x.IssueQty);
                    var dayTarget = g.Sum(x => x.DayTarget);

                    var manPower = Math.Round(g.Average(x => x.ManPower), 0);
                    var smv = g.Average(x => x.SMV);

                    return new DhuDryProcessSummaryDTO
                    {
                        ProcessModuleId = g.Key.ProcessModuleId,
                        ProcessModuleName = g.Key.ProcessModuleName,

                        WashProcessId = g.Key.WashProcessId,
                        ProcessName = g.Key.ProcessName,

                        PassQty = passQty,
                        DefectQty = defectQty,
                        RejectQty = rejectQty,
                        IssueQty = issueQty,

                        DayTarget = dayTarget,
                        ManPower = manPower,
                        SMV = smv,

                        DHU = passQty == 0
                            ? 0
                            : Math.Round(issueQty * 100m / passQty, 2),

                        PlanEff = manPower == 0 || smv == 0
                            ? 0
                            : Math.Round(dayTarget * smv * 100m / (11 * manPower * 60), 2),

                        ActualEff = manPower == 0 || smv == 0
                            ? 0
                            : Math.Round(passQty * smv * 100m / (11 * manPower * 60), 2)
                    };
                })
                .OrderBy(x => x.ProcessModuleName)
                .ThenBy(x => x.ProcessName)
                .ToList();
        }


        //public async Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        //{
        //    using var connection =
        //        _context.CreateConnection();

        //    var parameters = new
        //    {
        //        FromDate = filter.FromDate,
        //        ToDate = filter.ToDate,

        //        PlantIds = filter.PlantId != null && filter.PlantId.Any()
        //           ? string.Join(",", filter.PlantId)
        //           : null,

        //        UnitIds = filter.UnitId != null && filter.UnitId.Any()
        //           ? string.Join(",", filter.UnitId)
        //           : null,

        //        ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
        //           ? string.Join(",", filter.ProcessModuleId)
        //           : null,

        //        WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
        //           ? string.Join(",", filter.WashProcessId)
        //           : null,

        //        ShiftList = filter.Shift != null && filter.Shift.Any()
        //           ? string.Join(",", filter.Shift)
        //           : null
        //    };

        //    var result = await connection.QueryAsync<TopIssueDTO>(DryProcessSummaryQuery.GetTopIssues,parameters, commandTimeout: 300
        //    );

        //    return result;
        //}
        public async Task<IEnumerable<TopIssueDTO>> GetTopIssuesAsync(
    DryProcessSummaryFilterDto filter)
        {
            var selectedUnits = filter.UnitId ?? new List<int>();
            bool hasUnitFilter = selectedUnits.Any();

            bool needTpl = !hasUnitFilter || selectedUnits.Contains(TplUnitId);
            bool needTwl = !hasUnitFilter || selectedUnits.Contains(TwlUnitId);

            var allData = new List<TopIssueDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TplUnitId).ToList()
                        : null
                );

                var tplData = await QueryTopIssuesDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TwlUnitId).ToList()
                        : null
                );

                var twlData = await QueryTopIssuesDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateTopIssues(allData);
        }
        private async Task<IEnumerable<TopIssueDTO>> QueryTopIssuesDatabaseAsync(
    IDbConnection connection,
    DryProcessSummaryFilterDto filter)
        {
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

            return await connection.QueryAsync<TopIssueDTO>(
                DryProcessSummaryQuery.GetTopIssues,
                parameters,
                commandTimeout: 300
            );
        }
        private IEnumerable<TopIssueDTO> AggregateTopIssues(
    IEnumerable<TopIssueDTO> data)
        {
            return data
                .GroupBy(x => new
                {
                  
                    x.WashProcessId,
                    x.ProcessName,
                    x.WashProcessIssueId,
                    x.IssueName,
                })
                .Select(g => new TopIssueDTO
                {
                    WashProcessId = g.Key.WashProcessId,
                    ProcessName = g.Key.ProcessName,
                    WashProcessIssueId = g.Key.WashProcessIssueId,
                    IssueName = g.Key.IssueName,
                    IssueQty = g.Sum(x => x.IssueQty)

                })
                .OrderByDescending(x => x.IssueQty)
                .Take(10)
                .ToList();
        }

        //        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(
        //DryProcessSummaryFilterDto filter)
        //        {
        //            using var connection = _context.CreateConnection();

        //            var parameters = new
        //            {
        //                FromDate = filter.FromDate,
        //                ToDate = filter.ToDate,

        //                PlantIds = filter.PlantId != null && filter.PlantId.Any()
        //                    ? string.Join(",", filter.PlantId)
        //                    : null,

        //                UnitIds = filter.UnitId != null && filter.UnitId.Any()
        //                    ? string.Join(",", filter.UnitId)
        //                    : null,

        //                ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
        //                    ? string.Join(",", filter.ProcessModuleId)
        //                    : null,

        //                WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
        //                    ? string.Join(",", filter.WashProcessId)
        //                    : null,

        //                ShiftList = filter.Shift != null && filter.Shift.Any()
        //                    ? string.Join(",", filter.Shift)
        //                    : null
        //            };

        //            var result = await connection.QueryAsync<DhuDryProcessSummaryDTO>(
        //                DryProcessSummaryQuery.GetWetSummary,
        //                parameters, commandTimeout: 300
        //            );

        //            return result;
        //        }

        public async Task<IEnumerable<DhuDryProcessSummaryDTO>> GetWetProcessSummaryAsync(
    DryProcessSummaryFilterDto filter)
        {
            var selectedUnits = filter.UnitId ?? new List<int>();
            bool hasUnitFilter = selectedUnits.Any();

            bool needTpl = !hasUnitFilter || selectedUnits.Contains(TplUnitId);
            bool needTwl = !hasUnitFilter || selectedUnits.Contains(TwlUnitId);

            var allData = new List<DhuDryProcessSummaryDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TplUnitId).ToList()
                        : null
                );

                var tplData = await QueryWetSummaryDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TwlUnitId).ToList()
                        : null
                );

                var twlData = await QueryWetSummaryDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateSummary(allData);
        }

        private async Task<IEnumerable<DhuDryProcessSummaryDTO>> QueryWetSummaryDatabaseAsync(
    IDbConnection connection,
    DryProcessSummaryFilterDto filter)
        {
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

            return await connection.QueryAsync<DhuDryProcessSummaryDTO>(
                DryProcessSummaryQuery.GetWetSummary,
                parameters,
                commandTimeout: 300
            );
        }

        //public async Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(DryProcessSummaryFilterDto filter)
        //{
        //    using var connection =
        //        _context.CreateConnection();

        //    var parameters = new
        //    {
        //        FromDate = filter.FromDate,
        //        ToDate = filter.ToDate,

        //        PlantIds = filter.PlantId != null && filter.PlantId.Any()
        //           ? string.Join(",", filter.PlantId)
        //           : null,

        //        UnitIds = filter.UnitId != null && filter.UnitId.Any()
        //           ? string.Join(",", filter.UnitId)
        //           : null,

        //        ProcessModuleIds = filter.ProcessModuleId != null && filter.ProcessModuleId.Any()
        //           ? string.Join(",", filter.ProcessModuleId)
        //           : null,

        //        WashProcessIds = filter.WashProcessId != null && filter.WashProcessId.Any()
        //           ? string.Join(",", filter.WashProcessId)
        //           : null,

        //        ShiftList = filter.Shift != null && filter.Shift.Any()
        //           ? string.Join(",", filter.Shift)
        //           : null
        //    };

        //    var result = await connection.QueryAsync<TopIssueDTO>(DryProcessSummaryQuery.GetWetTopIssues, parameters, commandTimeout: 300
        //    );

        //    return result;
        //}
        public async Task<IEnumerable<TopIssueDTO>> GetWetTopIssuesAsync(
    DryProcessSummaryFilterDto filter)
        {
            var selectedUnits = filter.UnitId ?? new List<int>();
            bool hasUnitFilter = selectedUnits.Any();

            bool needTpl = !hasUnitFilter || selectedUnits.Contains(TplUnitId);
            bool needTwl = !hasUnitFilter || selectedUnits.Contains(TwlUnitId);

            var allData = new List<TopIssueDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TplUnitId).ToList()
                        : null
                );

                var tplData = await QueryWetTopIssuesDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasUnitFilter
                        ? selectedUnits.Where(x => x == TwlUnitId).ToList()
                        : null
                );

                var twlData = await QueryWetTopIssuesDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateTopIssues(allData);
        }
        private async Task<IEnumerable<TopIssueDTO>> QueryWetTopIssuesDatabaseAsync(
    IDbConnection connection,
    DryProcessSummaryFilterDto filter)
        {
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

            return await connection.QueryAsync<TopIssueDTO>(
                DryProcessSummaryQuery.GetWetTopIssues,
                parameters,
                commandTimeout: 300
            );
        }

    }
}
