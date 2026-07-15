using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;
using wsahRecieveDelivary.Common.Models;
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

        //private const int TplPlantId = 1;
        //private const int TwlPlantId = 2;

        private const int TplPlantId = 1;
        private const int TwlPlantId = 2;

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
            var selectedPlants = filter.PlantId ?? new List<int>();

            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessSummaryDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
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
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
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
      List<int>? plantIds)
        {
            return new DryProcessSummaryFilterDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantId = plantIds,
                UnitId = filter.UnitId,

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
            var selectedPlants = filter.PlantId ?? new List<int>();
            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<TopIssueDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
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
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
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
        //    private IEnumerable<TopIssueDTO> AggregateTopIssues(
        //IEnumerable<TopIssueDTO> data)
        //    {
        //        return data
        //            .GroupBy(x => new
        //            {

        //                x.WashProcessId,
        //                x.ProcessName,
        //                x.WashProcessIssueId,
        //                x.IssueName,
        //            })
        //            .Select(g => new TopIssueDTO
        //            {
        //                WashProcessId = g.Key.WashProcessId,
        //                ProcessName = g.Key.ProcessName,
        //                WashProcessIssueId = g.Key.WashProcessIssueId,
        //                IssueName = g.Key.IssueName,
        //                IssueQty = g.Sum(x => x.IssueQty)

        //            })
        //            .OrderByDescending(x => x.IssueQty)
        //            .Take(10)
        //            .ToList();
        //    }
        private IEnumerable<TopIssueDTO> AggregateTopIssues(IEnumerable<TopIssueDTO> data)
        {
            var aggregated = data
                .GroupBy(x => new
                {
                    x.WashProcessId,
                    x.ProcessName,
                    x.WashProcessIssueId,
                    x.IssueName
                })
                .Select(g => new TopIssueDTO
                {
                    WashProcessId = g.Key.WashProcessId,
                    ProcessName = g.Key.ProcessName,
                    WashProcessIssueId = g.Key.WashProcessIssueId,
                    IssueName = g.Key.IssueName,
                    IssueQty = g.Sum(x => x.IssueQty)
                })
                .ToList();

            return aggregated
                .GroupBy(x => new
                {
                    x.WashProcessId,
                    x.ProcessName
                })
                .SelectMany(g => g
                    .OrderByDescending(x => x.IssueQty)
                    .Take(3)
                )
                .OrderBy(x => x.ProcessName)
                .ThenByDescending(x => x.IssueQty)
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
            var selectedPlants = filter.PlantId ?? new List<int>();
            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessSummaryDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
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
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
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
            var selectedPlants = filter.PlantId ?? new List<int>();
            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<TopIssueDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
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
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
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

        //////////////DHU Dry Details  
        ///

        public async Task<PagedResult<DhuDryProcessDetailsDTO>>
    GetDryProcessDetailsAsync(DryProcessDetailsFilterDto filter)
        {
            var selectedPlants = filter.PlantId ?? new List<int>();

            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessDetailsDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
                        : null
                );

                var tplData = await QueryDatabaseDetailsAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
                        : null
                );

                var twlData = await QueryDatabaseDetailsAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            var pageNumber = filter.PageNumber <= 0
     ? 1
     : filter.PageNumber;

            var pageSize = filter.PageSize <= 0
                ? 10
                : filter.PageSize;

            var aggregatedData = AggregateDetails(allData)
                .OrderBy(x => x.ProcessModuleName)
                .ThenBy(x => x.StyleName)
                .ThenBy(x => x.FastReactNo)
                .ThenBy(x => x.WorkOrderNo)
                .ThenBy(x => x.ProcessName)
                .ToList();

            var totalCount = aggregatedData.Count;

            var items = aggregatedData
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<DhuDryProcessDetailsDTO>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        private async Task<IEnumerable<DhuDryProcessDetailsDTO>>
      QueryDatabaseDetailsAsync(
          IDbConnection connection,
          DryProcessDetailsFilterDto filter)
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

                ProcessModuleIds =
                    filter.ProcessModuleId != null &&
                    filter.ProcessModuleId.Any()
                        ? string.Join(",", filter.ProcessModuleId)
                        : null,

                WashProcessIds =
                    filter.WashProcessId != null &&
                    filter.WashProcessId.Any()
                        ? string.Join(",", filter.WashProcessId)
                        : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                    ? string.Join(",", filter.Shift)
                    : null,

                SearchText = string.IsNullOrWhiteSpace(filter.SearchText)
                    ? null
                    : filter.SearchText.Trim()
            };

            return await connection.QueryAsync<DhuDryProcessDetailsDTO>(
                DryProcessSummaryQuery.GetDetails,
                parameters,
                commandTimeout: 300
            );
        }
        private DryProcessDetailsFilterDto CloneFilter(
     DryProcessDetailsFilterDto filter,
     List<int>? plantIds)
        {
            return new DryProcessDetailsFilterDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantId = plantIds,
                UnitId = filter.UnitId,

                ProcessModuleId = filter.ProcessModuleId,
                WashProcessId = filter.WashProcessId,
                Shift = filter.Shift,

                SearchText = filter.SearchText,

                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize
            };
        }

        private IEnumerable<DhuDryProcessDetailsDTO> AggregateDetails(
            IEnumerable<DhuDryProcessDetailsDTO> data)
        {
            return data
                .GroupBy(x => new
                {
                    x.ProcessModuleId,
                    x.ProcessModuleName,
                    x.StyleName,
                    x.FastReactNo,
                    x.WorkOrderNo,
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

                    return new DhuDryProcessDetailsDTO
                    {
                        ProcessModuleId = g.Key.ProcessModuleId,
                        ProcessModuleName = g.Key.ProcessModuleName,
                        StyleName = g.Key.StyleName,
                        FastReactNo = g.Key.FastReactNo,
                        WorkOrderNo = g.Key.WorkOrderNo,

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



        //////////////DHU Wet Details  
        ///

        public async Task<PagedResult<DhuDryProcessDetailsDTO>>
    GetWetProcessDetailsAsync(DryProcessDetailsFilterDto filter)
        {
            var selectedPlants = filter.PlantId ?? new List<int>();

            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessDetailsDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
                        : null
                );

                var tplData = await QueryWetDatabaseDetailsAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
                        : null
                );

                var twlData = await QueryWetDatabaseDetailsAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            var pageNumber = filter.PageNumber <= 0
     ? 1
     : filter.PageNumber;

            var pageSize = filter.PageSize <= 0
                ? 10
                : filter.PageSize;

            var aggregatedData = AggregateDetails(allData)
                .OrderBy(x => x.ProcessModuleName)
                .ThenBy(x => x.StyleName)
                .ThenBy(x => x.FastReactNo)
                .ThenBy(x => x.WorkOrderNo)
                .ThenBy(x => x.ProcessName)
                .ToList();

            var totalCount = aggregatedData.Count;

            var items = aggregatedData
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedResult<DhuDryProcessDetailsDTO>
            {
                Items = items,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount
            };
        }

        private async Task<IEnumerable<DhuDryProcessDetailsDTO>>
      QueryWetDatabaseDetailsAsync(
          IDbConnection connection,
          DryProcessDetailsFilterDto filter)
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

                ProcessModuleIds =
                    filter.ProcessModuleId != null &&
                    filter.ProcessModuleId.Any()
                        ? string.Join(",", filter.ProcessModuleId)
                        : null,

                WashProcessIds =
                    filter.WashProcessId != null &&
                    filter.WashProcessId.Any()
                        ? string.Join(",", filter.WashProcessId)
                        : null,

                ShiftList = filter.Shift != null && filter.Shift.Any()
                    ? string.Join(",", filter.Shift)
                    : null,

                SearchText = string.IsNullOrWhiteSpace(filter.SearchText)
                    ? null
                    : filter.SearchText.Trim()
            };

            return await connection.QueryAsync<DhuDryProcessDetailsDTO>(
                DryProcessSummaryQuery.GetWetDetails,
                parameters,
                commandTimeout: 300
            );
        }
        

        //////////////DryProcess Hourly Details

        public async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetDryProcessHourlyDetailsAsync(
         DhuDryProcessHourlyDetailsFilterDto filter)
        {
            var selectedPlants = filter.PlantId ?? new List<int>();

            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessHourlyDetailsDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
                        : null
                );

                var tplData = await QueryHourlyDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
                        : null
                );

                var twlData = await QueryHourlyDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateHourlySummary(allData);
        }

        private async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> QueryHourlyDatabaseAsync(
            IDbConnection connection,
            DhuDryProcessHourlyDetailsFilterDto filter)
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

            return await connection.QueryAsync<DhuDryProcessHourlyDetailsDTO>(
                DryProcessSummaryQuery.GetDryProcessHourlyDetails,
                parameters,
                commandTimeout: 300
            );
        }

        private DhuDryProcessHourlyDetailsFilterDto CloneFilter(
     DhuDryProcessHourlyDetailsFilterDto filter,
      List<int>? plantIds)
        {
            return new DhuDryProcessHourlyDetailsFilterDto
            {
                FromDate = filter.FromDate,
                ToDate = filter.ToDate,

                PlantId = plantIds,
                UnitId = filter.UnitId,

                ProcessModuleId = filter.ProcessModuleId,
                WashProcessId = filter.WashProcessId,
                Shift = filter.Shift
            };
        }

        private IEnumerable<DhuDryProcessHourlyDetailsDTO> AggregateHourlySummary(
            IEnumerable<DhuDryProcessHourlyDetailsDTO> data)
        {
            return data
                .GroupBy(x => new
                {
                    x.ProcessModuleId,
                    x.ProcessModuleName,
                    x.WashProcessId,
                    x.ProcessName,
                    x.HourRange,
                    x.HourSlot
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

                    return new DhuDryProcessHourlyDetailsDTO
                    {
                        ProcessModuleId = g.Key.ProcessModuleId,
                        ProcessModuleName = g.Key.ProcessModuleName,

                        WashProcessId = g.Key.WashProcessId,
                        ProcessName = g.Key.ProcessName,
                        HourRange = g.Key.HourRange,
                        HourSlot = g.Key.HourSlot,
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

        //////wet process hourly details

        public async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> GetWetProcessHourlyDetailsAsync(
        DhuDryProcessHourlyDetailsFilterDto filter)
        {
            var selectedPlants = filter.PlantId ?? new List<int>();

            bool hasPlantFilter = selectedPlants.Any();

            bool needTpl = !hasPlantFilter || selectedPlants.Contains(TplPlantId);
            bool needTwl = !hasPlantFilter || selectedPlants.Contains(TwlPlantId);

            var allData = new List<DhuDryProcessHourlyDetailsDTO>();

            if (needTpl)
            {
                using var tplConnection = _context.CreateConnection();

                var tplFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TplPlantId).ToList()
                        : null
                );

                var tplData = await QueryWetHourlyDatabaseAsync(tplConnection, tplFilter);
                allData.AddRange(tplData);
            }

            if (needTwl)
            {
                using var twlConnection = _twlContext.CreateConnection();

                var twlFilter = CloneFilter(
                    filter,
                    hasPlantFilter
                        ? selectedPlants.Where(x => x == TwlPlantId).ToList()
                        : null
                );

                var twlData = await QueryWetHourlyDatabaseAsync(twlConnection, twlFilter);
                allData.AddRange(twlData);
            }

            return AggregateHourlySummary(allData);
        }

        private async Task<IEnumerable<DhuDryProcessHourlyDetailsDTO>> QueryWetHourlyDatabaseAsync(
            IDbConnection connection,
            DhuDryProcessHourlyDetailsFilterDto filter)
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

            return await connection.QueryAsync<DhuDryProcessHourlyDetailsDTO>(
                DryProcessSummaryQuery.GetWetProcessHourlyDetails,
                parameters,
                commandTimeout: 300
            );
        }

      
    }
}
