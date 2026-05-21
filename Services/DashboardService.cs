using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;
using wsahRecieveDelivary.Models.Enums;

namespace wsahRecieveDelivary.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;

        public DashboardService(ApplicationDbContext context)
        {
            _context = context;
        }


        //public async Task<List<DashboardDto>> GetDashboardDataAsync(
        //    DateTime? fromDate,
        //    DateTime? toDate,
        //    string? plant,
        //    string? unit,
        //    int? shift)
        //{
        //    try
        //    {
        //        var query =
        //            from wt in _context.WashTransactions.AsNoTracking()
        //            join ps in _context.ProcessStages
        //                on wt.ProcessStageId equals ps.Id
        //            join wo in _context.WorkOrders
        //                on wt.WorkOrderId equals wo.Id
        //            where wt.IsActive
        //            select new
        //            {
        //                wt,
        //                ps,
        //                wo
        //            };

        //        if (fromDate.HasValue)
        //            query = query.Where(x => x.wt.ShiftDate.Date >= fromDate.Value.Date);

        //        if (toDate.HasValue)
        //            query = query.Where(x => x.wt.ShiftDate.Date <= toDate.Value.Date);

        //        if (!string.IsNullOrEmpty(plant))
        //        {
        //            query = query.Where(x =>
        //                (plant == "TWL" && x.wo.Unit == "Unit TWL") ||
        //                (plant == "TPL" && x.wo.Unit != "Unit TWL"));
        //        }

        //        if (!string.IsNullOrEmpty(unit))
        //            query = query.Where(x => x.wo.Unit == unit);

        //        if (shift.HasValue)
        //        {
        //            var shiftType = (ShiftType)shift.Value;
        //            query = query.Where(x => x.wt.ShiftType == shiftType);
        //        }

        //        var groupedData = await query
        //            .GroupBy(x => new
        //            {
        //                x.wt.ShiftDate,
        //                x.wo.Unit,
        //                Plant = x.wo.Unit == "Unit TWL" ? "TWL" : "TPL",
        //                x.wt.ShiftType,
        //                x.wt.TransactionType,

        //                // ✅ MAGIC HAPPENS HERE: Combine 7 & 8
        //                ProcessStageId = (x.wt.ProcessStageId == 7 || x.wt.ProcessStageId == 8)
        //                                    ? 0  // Assign a dummy ID (like 0) for the combined row
        //                                    : x.wt.ProcessStageId,

        //                ProcessStageName = (x.wt.ProcessStageId == 7 || x.wt.ProcessStageId == 8)
        //                                    ? "Final Wash Dryer"
        //                                    : x.ps.Name
        //            })
        //            .Select(g => new
        //            {
        //                ShiftDate = g.Key.ShiftDate,
        //                Plant = g.Key.Plant,
        //                Unit = g.Key.Unit,
        //                ShiftType = g.Key.ShiftType,
        //                TransactionType = g.Key.TransactionType,
        //                ProcessStageId = g.Key.ProcessStageId,
        //                ProcessStageName = g.Key.ProcessStageName,
        //                TotalQuantity = g.Sum(x => (long)x.wt.Quantity)
        //            })
        //            .OrderBy(x => x.ShiftDate)
        //            .ThenBy(x => x.Plant)
        //            .ThenBy(x => x.Unit)
        //            .ThenBy(x => x.ProcessStageName) // ✅ Changed from ProcessStageId to Name for correct alphabetical sorting
        //            .ThenBy(x => x.TransactionType)
        //            .ToListAsync();

        //        var result = groupedData.Select(g => new DashboardDto
        //        {
        //            ShiftDate = g.ShiftDate,
        //            Plant = g.Plant,
        //            Unit = g.Unit,
        //            ShiftType = g.ShiftType.ToString(),
        //            ProcessStageId = g.ProcessStageId,      // Will be 0 for the combined stage
        //            ProcessStageName = g.ProcessStageName,  // Will be "Final Wash Dryer"
        //            TransactionType = g.TransactionType.ToString(),
        //            TotalQuantity = g.TotalQuantity
        //        }).ToList();

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}


        public async Task<List<DashboardDto>> GetDashboardDataAsync(
    DateOnly? fromDate,
    DateOnly? toDate,
    string? plant,
    string? unit,
    int? shift,
    ClaimsPrincipal? user)
        {
            try
            {
                var query =
                    from wt in _context.WashTransactions.AsNoTracking()
                    join ps in _context.ProcessStages
                        on wt.ProcessStageId equals ps.Id
                    join wo in _context.WorkOrders
                        on wt.WorkOrderId equals wo.Id
                    where wt.IsActive
                    select new
                    {
                        wt,
                        ps,
                        wo
                    };

                //if (fromDate.HasValue)
                //    query = query.Where(x => x.wt.ShiftDate.Date >= fromDate.Value.Date);

                //if (toDate.HasValue)
                //    query = query.Where(x => x.wt.ShiftDate.Date <= toDate.Value.Date);
                if (fromDate.HasValue)
                    query = query.Where(x => x.wt.ShiftDate >= fromDate);

                if (toDate.HasValue)
                    query = query.Where(x => x.wt.ShiftDate <= toDate);

                if (!string.IsNullOrEmpty(plant))
                {
                    query = query.Where(x =>
                        (plant == "TWL" && x.wo.Unit == "Unit TWL") ||
                        (plant == "TPL" && x.wo.Unit != "Unit TWL"));
                }

                if (!string.IsNullOrEmpty(unit))
                    query = query.Where(x => x.wo.Unit == unit);

                if (shift.HasValue)
                {
                    var shiftType = (ShiftType)shift.Value;
                    query = query.Where(x => x.wt.ShiftType == shiftType);
                }

                if (user != null)
                {
                    var isAdmin = user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
                    var isDashboardUser = user.Claims.Any(c => c.Type == ClaimTypes.Role && (c.Value == "Incharge" || c.Value == "Planner"));

                    if (!isAdmin)
                    {
                        if (isDashboardUser)
                        {
                            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                            var userAssigns = (from ua in _context.Set<UserAssign>()
                                               where ua.UserId == userId && !ua.isDeleted && ua.PlantId != null && ua.UnitId != null
                                               join p in _context.Plants on ua.PlantId equals p.Id
                                               join un in _context.Units on ua.UnitId equals un.Id
                                               join u in _context.Users on ua.UserId equals u.Id
                                               where u.IsActive
                                               select new
                                               {
                                                   ua.PlantId,
                                                   PlantName = p.Name,
                                                   ua.UnitId,
                                                   UnitName = un.Name
                                               }).Distinct().ToList();

                            if (userAssigns.Any())
                            {
                                var plantIds = userAssigns.Select(ua => ua.PlantName).ToList();
                                var unitIds = userAssigns.Select(ua => ua.UnitName).ToList();

                                query = query.Where(x => plantIds.Contains(x.wo.Unit == "Unit TWL" ? "TWL" : "TPL") && unitIds.Contains(x.wo.Unit));
                            }
                            else
                            {
                                return new List<DashboardDto>();
                            }
                        }
                        else
                        {
                            return new List<DashboardDto>();
                        }
                    }
                }

                var groupedData = await query
                    .GroupBy(x => new
                    {
                        x.wt.ShiftDate,
                        x.wo.Unit,
                        Plant = x.wo.Unit == "Unit TWL" ? "TWL" : "TPL",
                        x.wt.ShiftType,
                        x.wt.TransactionType,

                        //ProcessStageId = (x.wt.ProcessStageId == 7 || x.wt.ProcessStageId == 8)
                        //                    ? 0
                        //                    : x.wt.ProcessStageId,
                        ProcessStageId = x.wt.ProcessStageId,

                        //ProcessStageName = (x.wt.ProcessStageId == 7 || x.wt.ProcessStageId == 8)
                        //                    ? "Final Wash Dryer"
                        //                    : x.ps.Name
                        ProcessStageName =  x.ps.Name
                    })
                    .Select(g => new
                    {
                        ShiftDate = g.Key.ShiftDate,
                        Plant = g.Key.Plant,
                        Unit = g.Key.Unit,
                        ShiftType = g.Key.ShiftType,
                        TransactionType = g.Key.TransactionType,
                        ProcessStageId = g.Key.ProcessStageId,
                        ProcessStageName = g.Key.ProcessStageName,
                        TotalQuantity = g.Sum(x => (long)x.wt.Quantity)
                    })
                    .OrderBy(x => x.ShiftDate)
                    .ThenBy(x => x.Plant)
                    .ThenBy(x => x.Unit)
                    .ThenBy(x => x.ProcessStageName)
                    .ThenBy(x => x.TransactionType)
                    .ToListAsync();

                var result = groupedData.Select(g => new DashboardDto
                {
                    ShiftDate = g.ShiftDate,
                    Plant = g.Plant,
                    Unit = g.Unit,
                    ShiftType = g.ShiftType.ToString(),
                    ProcessStageId = g.ProcessStageId,
                    ProcessStageName = g.ProcessStageName,
                    TransactionType = g.TransactionType.ToString(),
                    TotalQuantity = g.TotalQuantity
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<DashboardDetailsResponseDto> GetDashboardDetailsAsync(
            DateOnly? fromDate,
            DateOnly? toDate,
            string? plant,
            string? unit,
            int? shift,
            List<int>? processStageIds,
            string? search,
            int page,
            int pageSize,
            ClaimsPrincipal? user)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 25;
                if (pageSize > 100) pageSize = 100;

                var query =
                    from wt in _context.WashTransactions.AsNoTracking()
                    join ps in _context.ProcessStages
                        on wt.ProcessStageId equals ps.Id
                    join wo in _context.WorkOrders
                        on wt.WorkOrderId equals wo.Id
                    where wt.IsActive
                    select new
                    {
                        wt,
                        ps,
                        wo
                    };

                if (fromDate.HasValue)
                {
                    query = query.Where(x => x.wt.ShiftDate >= fromDate);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(x => x.wt.ShiftDate <= toDate);
                }

                if (!string.IsNullOrEmpty(plant))
                {
                    query = query.Where(x =>
                        (plant == "TWL" && x.wo.Unit == "Unit TWL") ||
                        (plant == "TPL" && x.wo.Unit != "Unit TWL"));
                }

                if (!string.IsNullOrEmpty(unit))
                {
                    query = query.Where(x => x.wo.Unit == unit);
                }

                if (shift.HasValue)
                {
                    var shiftType = (ShiftType)shift.Value;
                    query = query.Where(x => x.wt.ShiftType == shiftType);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(x =>
                        (x.wo.WorkOrderNo != null && x.wo.WorkOrderNo.ToLower().Contains(searchTerm)) ||
                        (x.wo.Buyer != null && x.wo.Buyer.ToLower().Contains(searchTerm)) ||
                        (x.wo.StyleName != null && x.wo.StyleName.ToLower().Contains(searchTerm)) ||
                        (x.wo.FastReactNo != null && x.wo.FastReactNo.ToLower().Contains(searchTerm)));
                }

                if (processStageIds != null && processStageIds.Any())
                {
                    query = query.Where(x => processStageIds.Contains(x.wt.ProcessStageId));
                }

                // User assignment filtering
                if (user != null)
                {
                    var isAdmin = user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
                    var isDashboardUser = user.Claims.Any(c => c.Type == ClaimTypes.Role && (c.Value == "Incharge" || c.Value == "Planner"));

                    if (!isAdmin)
                    {
                        if (isDashboardUser)
                        {
                            var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                            var userAssigns = (from ua in _context.Set<UserAssign>()
                                               join p in _context.Plants on ua.PlantId equals p.Id
                                               join un in _context.Units on ua.UnitId equals un.Id
                                               join u in _context.Users on ua.UserId equals u.Id
                                               where ua.UserId == userId && !ua.isDeleted && u.IsActive
                                               select new
                                               {
                                                   ua.PlantId,
                                                   PlantName = p.Name,
                                                   ua.UnitId,
                                                   UnitName = un.Name
                                               }).Distinct().ToList();

                            if (userAssigns.Any())
                            {
                                var plantIds = userAssigns.Select(ua => ua.PlantName).ToList();
                                var unitIds = userAssigns.Select(ua => ua.UnitName).ToList();

                                query = query.Where(x => plantIds.Contains(x.wo.Unit == "Unit TWL" ? "TWL" : "TPL") && unitIds.Contains(x.wo.Unit));
                            }
                            else
                            {
                                return new DashboardDetailsResponseDto
                                {
                                    Success = true,
                                    Message = "DashboardUser has no plant/unit assignments",
                                    Data = new List<DashboardDetailsDto>(),
                                    Pagination = new PaginationMetadata
                                    {
                                        CurrentPage = page,
                                        PageSize = pageSize,
                                        TotalRecords = 0,
                                        TotalPages = 0,
                                        HasPrevious = false,
                                        HasNext = false
                                    }
                                };
                            }
                        }
                        else
                        {
                            return new DashboardDetailsResponseDto
                            {
                                Success = true,
                                Message = "User has no plant/unit access",
                                Data = new List<DashboardDetailsDto>(),
                                Pagination = new PaginationMetadata
                                {
                                    CurrentPage = page,
                                    PageSize = pageSize,
                                    TotalRecords = 0,
                                    TotalPages = 0,
                                    HasPrevious = false,
                                    HasNext = false
                                }
                            };
                        }
                    }
                }

                var totalCount = await query
                    .GroupBy(x => new
                    {
                        x.wo.Factory,
                        x.wo.Unit,
                        x.wo.WorkOrderNo,
                        x.wo.BuyerDepartment,
                        x.wo.StyleName,
                        x.wo.FastReactNo,
                        x.wo.WashTargetDate,
                        //StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
                        x.ps.Name,
                        x.wt.ShiftType,
                        x.wt.TransactionType
                    })
                    .CountAsync();

                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var groupedData = await query
                    .GroupBy(x => new
                    {
                        x.wo.Factory,
                        x.wo.Unit,
                        x.wo.WorkOrderNo,
                        x.wo.BuyerDepartment,
                        x.wo.StyleName,
                        x.wo.FastReactNo,
                        x.wo.WashTargetDate,
                        // StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
                        x.ps.Name,
                        x.wt.ShiftType,
                        x.wt.TransactionType
                    })
                    .Select(g => new
                    {
                        Factory = g.Key.Factory ?? "",
                        Unit = g.Key.Unit ?? "",
                        WorkOrderNo = g.Key.WorkOrderNo ?? "",
                        BuyerDepartment = g.Key.BuyerDepartment,
                        StyleName = g.Key.StyleName ?? "",
                        FastReactNo = g.Key.FastReactNo,
                        OrderQuantity = g.Select(x => x.wo.OrderQuantity).FirstOrDefault() ?? 0,
                        // OrderQuantity = g.Sum(x => x.wo.OrderQuantity ?? 0),
                        WashTargetDate = g.Key.WashTargetDate,
                        //TotalWashReceived = g.Sum(x => x.wo.TotalWashReceived ?? 0),
                        //TotalWashDelivery = g.Sum(x => x.wo.TotalWashDelivery ?? 0),
                        TotalWashReceived = g.Select(x => x.wo.TotalWashReceived).FirstOrDefault() ?? 0,
                        TotalWashDelivery = g.Select(x => x.wo.TotalWashDelivery).FirstOrDefault() ?? 0,
                        ShiftType = g.Key.ShiftType,
                        StageName = g.Key.Name,
                        TransactionType = g.Key.TransactionType,
                        Quantity = g.Sum(x => (long)x.wt.Quantity)
                    })
                    .OrderBy(x => x.WorkOrderNo)
                    .ThenBy(x => x.ShiftType)
                    .ThenBy(x => x.StageName)
                    .ThenBy(x => x.TransactionType)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var result = groupedData.Select(g => new DashboardDetailsDto
                {
                    Factory = g.Factory,
                    Unit = g.Unit,
                    WorkOrderNo = g.WorkOrderNo,
                    BuyerDepartment = g.BuyerDepartment,
                    StyleName = g.StyleName,
                    FastReactNo = g.FastReactNo,
                    OrderQuantity = g.OrderQuantity,
                    WashTargetDate = g.WashTargetDate,
                    TotalWashReceived = g.TotalWashReceived,
                    TotalWashDelivery = g.TotalWashDelivery,
                    ShiftType = g.ShiftType.ToString(),
                    StageName = g.StageName,
                    TransactionType = g.TransactionType.ToString(),
                    Quantity = (int)g.Quantity
                }).ToList();

                return new DashboardDetailsResponseDto
                {
                    Success = true,
                    Message = "Data retrieved successfully",
                    Data = result,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalRecords = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = page > 1,
                        HasNext = page < totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                return new DashboardDetailsResponseDto
                {
                    Success = false,
                    Message = $"Error retrieving data: {ex.Message}",
                    Data = new List<DashboardDetailsDto>(),
                    Pagination = new PaginationMetadata()
                };
            }
        }


        //    public async Task<DashboardDetailsResponseDto> GetDashboardDetailsAsync(
        //DateTime? fromDate,
        //DateTime? toDate,
        //string? plant,
        //string? unit,
        //int? shift,
        //List<int>? processStageIds,
        //string? search,
        //int page,
        //int pageSize,
        //ClaimsPrincipal? user)
        //    {
        //        try
        //        {
        //            // ✅ Set default values for pagination
        //            if (page < 1) page = 1;
        //            if (pageSize < 1) pageSize = 25;
        //            if (pageSize > 100) pageSize = 100;



        //            var query =
        //                from wt in _context.WashTransactions.AsNoTracking()
        //                join ps in _context.ProcessStages
        //                    on wt.ProcessStageId equals ps.Id
        //                join wo in _context.WorkOrders
        //                    on wt.WorkOrderId equals wo.Id
        //                where wt.IsActive
        //                select new
        //                {
        //                    wt,
        //                    ps,
        //                    wo
        //                };

        //            if (fromDate.HasValue)
        //            {
        //                query = query.Where(x => x.wt.ShiftDate.Date >= fromDate.Value.Date);

        //            }

        //            if (toDate.HasValue)
        //            {
        //                query = query.Where(x => x.wt.ShiftDate.Date <= toDate.Value.Date);

        //            }

        //            if (!string.IsNullOrEmpty(plant))
        //            {
        //                query = query.Where(x =>
        //                    (plant == "TWL" && x.wo.Unit == "Unit TWL") ||
        //                    (plant == "TPL" && x.wo.Unit != "Unit TWL"));
        //            }

        //            if (!string.IsNullOrEmpty(unit))
        //            {
        //                query = query.Where(x => x.wo.Unit == unit);

        //            }

        //            if (shift.HasValue)
        //            {
        //                var shiftType = (ShiftType)shift.Value;
        //                query = query.Where(x => x.wt.ShiftType == shiftType);

        //            }

        //            if (!string.IsNullOrEmpty(search))
        //            {
        //                var searchTerm = search.ToLower();
        //                query = query.Where(x =>
        //                    (x.wo.WorkOrderNo != null && x.wo.WorkOrderNo.ToLower().Contains(searchTerm)) ||
        //                    (x.wo.Buyer != null && x.wo.Buyer.ToLower().Contains(searchTerm)) ||
        //                    (x.wo.StyleName != null && x.wo.StyleName.ToLower().Contains(searchTerm)) ||
        //                    (x.wo.FastReactNo != null && x.wo.FastReactNo.ToLower().Contains(searchTerm)));

        //            }

        //            if (processStageIds != null && processStageIds.Any())
        //            {
        //                query = query.Where(x => processStageIds.Contains(x.wt.ProcessStageId));

        //            }

        //            // User assignment filtering
        //            if (user != null)
        //            {
        //                var isAdmin = user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
        //                var isDashboardUser = user.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Incharge");

        //                if (!isAdmin)
        //                {
        //                    if (isDashboardUser)
        //                    {
        //                        var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        //                        var userAssigns = (from ua in _context.Set<UserAssign>()
        //                                           join u in _context.Users on ua.UserId equals u.Id
        //                                           join un in _context.Units on ua.UnitId equals un.Id
        //                                           join p in _context.Plants on un.PlantId equals p.Id
        //                                           where ua.UserId == userId && !ua.isDeleted && u.IsActive
        //                                           select new
        //                                           {
        //                                               ua.PlantId,
        //                                               PlantName = p.Name,
        //                                               ua.UnitId,
        //                                               UnitName = un.Name
        //                                           }).Distinct().ToList();

        //                        if (userAssigns.Any())
        //                        {
        //                            var plantIds = userAssigns.Select(ua => ua.PlantName).ToList();
        //                            var unitIds = userAssigns.Select(ua => ua.UnitName).ToList();

        //                            query = query.Where(x => plantIds.Contains(x.wo.Unit == "Unit TWL" ? "TWL" : "TPL") && unitIds.Contains(x.wo.Unit));
        //                        }
        //                        else
        //                        {
        //                            return new DashboardDetailsResponseDto
        //                            {
        //                                Success = true,
        //                                Message = "DashboardUser has no plant/unit assignments",
        //                                Data = new List<DashboardDetailsDto>(),
        //                                Pagination = new PaginationMetadata
        //                                {
        //                                    CurrentPage = page,
        //                                    PageSize = pageSize,
        //                                    TotalRecords = 0,
        //                                    TotalPages = 0,
        //                                    HasPrevious = false,
        //                                    HasNext = false
        //                                }
        //                            };
        //                        }
        //                    }
        //                    else
        //                    {
        //                        return new DashboardDetailsResponseDto
        //                        {
        //                            Success = true,
        //                            Message = "User has no plant/unit access",
        //                            Data = new List<DashboardDetailsDto>(),
        //                            Pagination = new PaginationMetadata
        //                            {
        //                                CurrentPage = page,
        //                                PageSize = pageSize,
        //                                TotalRecords = 0,
        //                                TotalPages = 0,
        //                                HasPrevious = false,
        //                                HasNext = false
        //                            }
        //                        };
        //                    }
        //                }
        //            }

        //            // Get total count for pagination
        //            var totalCount = await query
        //                .GroupBy(x => new
        //                {
        //                    x.wo.Factory,
        //                    x.wo.Unit,
        //                    x.wo.WorkOrderNo,
        //                    x.wo.BuyerDepartment,
        //                    x.wo.StyleName,
        //                    x.wo.FastReactNo,
        //                    x.wo.WashTargetDate,
        //                    StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
        //                    x.wt.ShiftType,
        //                    x.wt.TransactionType
        //                })
        //                .CountAsync();



        //            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        //            // Get paginated data
        //            var groupedData = await query
        //                .GroupBy(x => new
        //                {
        //                    x.wo.Factory,
        //                    x.wo.Unit,
        //                    x.wo.WorkOrderNo,
        //                    x.wo.BuyerDepartment,
        //                    x.wo.StyleName,
        //                    x.wo.FastReactNo,
        //                    x.wo.WashTargetDate,
        //                    StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
        //                    x.wt.ShiftType,
        //                    x.wt.TransactionType
        //                })
        //                .Select(g => new
        //                {
        //                    Factory = g.Key.Factory ?? "",
        //                    Unit = g.Key.Unit ?? "",
        //                    WorkOrderNo = g.Key.WorkOrderNo ?? "",
        //                    BuyerDepartment = g.Key.BuyerDepartment,
        //                    StyleName = g.Key.StyleName ?? "",
        //                    FastReactNo = g.Key.FastReactNo,
        //                    OrderQuantity = g.Sum(x => x.wo.OrderQuantity ?? 0),
        //                    WashTargetDate = g.Key.WashTargetDate,
        //                    TotalWashReceived = g.Sum(x => x.wo.TotalWashReceived ?? 0),
        //                    TotalWashDelivery = g.Sum(x => x.wo.TotalWashDelivery ?? 0),
        //                    ShiftType = g.Key.ShiftType,
        //                    StageName = g.Key.StageName,
        //                    TransactionType = g.Key.TransactionType,
        //                    Quantity = g.Sum(x => (long)x.wt.Quantity)
        //                })
        //                .OrderBy(x => x.WorkOrderNo)
        //                .ThenBy(x => x.ShiftType)
        //                .ThenBy(x => x.StageName)
        //                .ThenBy(x => x.TransactionType)
        //                .Skip((page - 1) * pageSize)
        //                .Take(pageSize)
        //                .ToListAsync();



        //            // Convert enums to strings client-side
        //            var result = groupedData.Select(g => new DashboardDetailsDto
        //            {
        //                Factory = g.Factory,
        //                Unit = g.Unit,
        //                WorkOrderNo = g.WorkOrderNo,
        //                BuyerDepartment = g.BuyerDepartment,
        //                StyleName = g.StyleName,
        //                FastReactNo = g.FastReactNo,
        //                OrderQuantity = g.OrderQuantity,
        //                WashTargetDate = g.WashTargetDate,
        //                TotalWashReceived = g.TotalWashReceived,
        //                TotalWashDelivery = g.TotalWashDelivery,
        //                ShiftType = g.ShiftType.ToString(),
        //                StageName = g.StageName,
        //                TransactionType = g.TransactionType.ToString(),
        //                Quantity = (int)g.Quantity
        //            }).ToList();

        //            return new DashboardDetailsResponseDto
        //            {
        //                Success = true,
        //                Message = "Data retrieved successfully",
        //                Data = result,
        //                Pagination = new PaginationMetadata
        //                {
        //                    CurrentPage = page,
        //                    PageSize = pageSize,
        //                    TotalRecords = totalCount,
        //                    TotalPages = totalPages,
        //                    HasPrevious = page > 1,
        //                    HasNext = page < totalPages
        //                }
        //            };
        //        }
        //        catch (Exception ex)
        //        {


        //            return new DashboardDetailsResponseDto
        //            {
        //                Success = false,
        //                Message = $"Error retrieving data: {ex.Message}",
        //                Data = new List<DashboardDetailsDto>(),
        //                Pagination = new PaginationMetadata()

        //            };
        //        }
        //    }



        //   public async Task<DashboardDetailsResponseDto> GetDashboardDetailsAsync(
        //DateTime? fromDate,
        //DateTime? toDate,
        //string? factory,
        //string? unit,
        //int? shift,
        //List<int>? processStageIds,
        //string? search,
        //int page,
        //int pageSize)
        //   {
        //       try
        //       {
        //           // ✅ Set default values for pagination
        //           if (page < 1) page = 1;
        //           if (pageSize < 1) pageSize = 25;
        //           if (pageSize > 100) pageSize = 100;

        //           Console.WriteLine("📊 GetDashboardDetailsAsync called");
        //           Console.WriteLine($"   Page: {page}, PageSize: {pageSize}");
        //           Console.WriteLine($"   FromDate: {fromDate}, ToDate: {toDate}");
        //           Console.WriteLine($"   Factory: {factory}, Unit: {unit}, Shift: {shift}");
        //           Console.WriteLine($"   Search: {search}");

        //           var query =
        //               from wt in _context.WashTransactions.AsNoTracking()
        //               join ps in _context.ProcessStages
        //                   on wt.ProcessStageId equals ps.Id
        //               join wo in _context.WorkOrders
        //                   on wt.WorkOrderId equals wo.Id
        //               where wt.IsActive
        //               select new
        //               {
        //                   wt,
        //                   ps,
        //                   wo
        //               };

        //           if (fromDate.HasValue)
        //           {
        //               query = query.Where(x => x.wt.ShiftDate.Date >= fromDate.Value.Date);
        //               Console.WriteLine($"   Applied FromDate filter: {fromDate.Value.Date:yyyy-MM-dd}");
        //           }

        //           if (toDate.HasValue)
        //           {
        //               query = query.Where(x => x.wt.ShiftDate.Date <= toDate.Value.Date);
        //               Console.WriteLine($"   Applied ToDate filter: {toDate.Value.Date:yyyy-MM-dd}");
        //           }

        //           if (!string.IsNullOrEmpty(factory))
        //           {
        //               query = query.Where(x => x.wo.Factory == factory);
        //               Console.WriteLine($"   Applied Factory filter: {factory}");
        //           }

        //           if (!string.IsNullOrEmpty(unit))
        //           {
        //               query = query.Where(x => x.wo.Unit == unit);
        //               Console.WriteLine($"   Applied Unit filter: {unit}");
        //           }

        //           if (shift.HasValue)
        //           {
        //               var shiftType = (ShiftType)shift.Value;
        //               query = query.Where(x => x.wt.ShiftType == shiftType);
        //               Console.WriteLine($"   Applied Shift filter: {shiftType}");
        //           }

        //           if (!string.IsNullOrEmpty(search))
        //           {
        //               var searchTerm = search.ToLower();
        //               query = query.Where(x =>
        //                   (x.wo.WorkOrderNo != null && x.wo.WorkOrderNo.ToLower().Contains(searchTerm)) ||
        //                   (x.wo.Buyer != null && x.wo.Buyer.ToLower().Contains(searchTerm)) ||
        //                   (x.wo.StyleName != null && x.wo.StyleName.ToLower().Contains(searchTerm)) ||
        //                   (x.wo.FastReactNo != null && x.wo.FastReactNo.ToLower().Contains(searchTerm)));
        //               Console.WriteLine($"   Applied Search filter: {search}");
        //           }

        //           if (processStageIds != null && processStageIds.Any())
        //           {
        //               query = query.Where(x => processStageIds.Contains(x.wt.ProcessStageId));
        //               Console.WriteLine($"   Applied ProcessStageIds filter: {string.Join(",", processStageIds)}");
        //           }

        //           // Get total count for pagination
        //           var totalCount = await query
        //               .GroupBy(x => new
        //               {
        //                   x.wo.Factory,
        //                   x.wo.Unit,
        //                   x.wo.WorkOrderNo,
        //                   x.wo.BuyerDepartment,
        //                   x.wo.StyleName,
        //                   x.wo.FastReactNo,
        //                   x.wo.WashTargetDate,
        //                   StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
        //                   x.wt.ShiftType,
        //                   x.wt.TransactionType
        //               })
        //               .CountAsync();

        //           Console.WriteLine($"   Total records: {totalCount}");

        //           var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        //           // Get paginated data
        //           var groupedData = await query
        //               .GroupBy(x => new
        //               {
        //                   x.wo.Factory,
        //                   x.wo.Unit,
        //                   x.wo.WorkOrderNo,
        //                   x.wo.BuyerDepartment,
        //                   x.wo.StyleName,
        //                   x.wo.FastReactNo,
        //                   x.wo.WashTargetDate,
        //                   StageName = x.ps.Id == 7 || x.ps.Id == 8 ? "Final Wash Dryer" : x.ps.Name,
        //                   x.wt.ShiftType,
        //                   x.wt.TransactionType
        //               })
        //               .Select(g => new
        //               {
        //                   Factory = g.Key.Factory ?? "",
        //                   Unit = g.Key.Unit ?? "",
        //                   WorkOrderNo = g.Key.WorkOrderNo ?? "",
        //                   BuyerDepartment = g.Key.BuyerDepartment,
        //                   StyleName = g.Key.StyleName ?? "",
        //                   FastReactNo = g.Key.FastReactNo,
        //                   OrderQuantity = g.Sum(x => x.wo.OrderQuantity ?? 0),
        //                   WashTargetDate = g.Key.WashTargetDate,
        //                   TotalWashReceived = g.Sum(x => x.wo.TotalWashReceived ?? 0),
        //                   TotalWashDelivery = g.Sum(x => x.wo.TotalWashDelivery ?? 0),
        //                   ShiftType = g.Key.ShiftType,
        //                   StageName = g.Key.StageName,
        //                   TransactionType = g.Key.TransactionType,
        //                   Quantity = g.Sum(x => (long)x.wt.Quantity)
        //               })
        //               .OrderBy(x => x.WorkOrderNo)
        //               .ThenBy(x => x.ShiftType)
        //               .ThenBy(x => x.StageName)
        //               .ThenBy(x => x.TransactionType)
        //               .Skip((page - 1) * pageSize)
        //               .Take(pageSize)
        //               .ToListAsync();

        //           Console.WriteLine($"   Retrieved {groupedData.Count} records for page {page}");

        //           // Convert enums to strings client-side
        //           var result = groupedData.Select(g => new DashboardDetailsDto
        //           {
        //               Factory = g.Factory,
        //               Unit = g.Unit,
        //               WorkOrderNo = g.WorkOrderNo,
        //               BuyerDepartment = g.BuyerDepartment,
        //               StyleName = g.StyleName,
        //               FastReactNo = g.FastReactNo,
        //               OrderQuantity = g.OrderQuantity,
        //               WashTargetDate = g.WashTargetDate,
        //               TotalWashReceived = g.TotalWashReceived,
        //               TotalWashDelivery = g.TotalWashDelivery,
        //               ShiftType = g.ShiftType.ToString(),
        //               StageName = g.StageName,
        //               TransactionType = g.TransactionType.ToString(),
        //               Quantity = (int)g.Quantity
        //           }).ToList();

        //           return new DashboardDetailsResponseDto
        //           {
        //               Success = true,
        //               Message = "Data retrieved successfully",
        //               Data = result,
        //               Pagination = new PaginationMetadata
        //               {
        //                   CurrentPage = page,
        //                   PageSize = pageSize,
        //                   TotalRecords = totalCount,
        //                   TotalPages = totalPages,
        //                   HasPrevious = page > 1,
        //                   HasNext = page < totalPages
        //               }
        //           };
        //       }
        //       catch (Exception ex)
        //       {
        //           Console.WriteLine($"❌ Error in GetDashboardDetailsAsync: {ex.Message}");
        //           Console.WriteLine($"   Stack: {ex.StackTrace}");

        //           return new DashboardDetailsResponseDto
        //           {
        //               Success = false,
        //               Message = $"Error retrieving data: {ex.Message}",
        //               Data = new List<DashboardDetailsDto>(),
        //               Pagination = new PaginationMetadata()

        //           };
        //       }
        //   }

        public async Task<List<PlantUnitDto>> GetPlantUnitListAsync()
        {
            try
            {
                var result = await (from p in _context.Plants
                                    join u in _context.Units on p.Id equals u.PlantId
                                    where p.isDeleted == false && u.IsDeleted == false
                                    select new PlantUnitDto
                                    {
                                        PlantId = p.Id,
                                        PlantName = p.Name,
                                        UnitId = u.Id,
                                        UnitName = u.Name
                                    }).ToListAsync();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<List<GetWashMachineDto>> GetMachineListAsync(int? plantId = null, int? unitId = null)
        {
            try
            {




                var query = from m in _context.WashMachine
                            where !m.IsDeleted
                            join ma in _context.WashMachineAssign on m.Id equals ma.MachineId into maGroup
                            from ma in maGroup.DefaultIfEmpty()
                            join p in _context.Plants on ma.PlantId equals p.Id into pGroup
                            from p in pGroup.DefaultIfEmpty()
                            join u in _context.Units on ma.UnitId equals u.Id into uGroup
                            from u in uGroup.DefaultIfEmpty()
                            where !ma.IsDeleted || ma == null
                            select new GetWashMachineDto
                            {
                                Id = m.Id,
                                MachineCode = m.MachineCode,
                                Brand = m.Brand,
                                Model = m.Model,
                                PlantName = p.Name,
                                UnitName = u.Name
                            };

                if (plantId.HasValue)
                {
                    query = query.Where(x => x.PlantName == (from p in _context.Plants where p.Id == plantId select p.Name).FirstOrDefault());
                }

                if (unitId.HasValue)
                {
                    query = query.Where(x => x.UnitName == (from u in _context.Units where u.Id == unitId select u.Name).FirstOrDefault());
                }

                var machines = await query.ToListAsync();
                return machines;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


    }
}
