using Microsoft.EntityFrameworkCore;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Services
{
    public class WashPlanService : IWashPlan
    {
        private readonly ApplicationDbContext _context;

        public WashPlanService(ApplicationDbContext context)
        {
            _context = context;
        }

        private DateTime GetBdTime()
        {
            TimeZoneInfo bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            return TimeZoneInfo.ConvertTime(DateTime.UtcNow, bdTimeZone);
        }

        //public async Task<List<GetWashMachineDto>> GetMachineListAsync(int? plantId = null, int? unitId = null)
        //{
        //    try
        //    {




        //        var query = from m in _context.WashMachine
        //                    where !m.IsDeleted
        //                    join ma in _context.WashMachineAssign on m.Id equals ma.MachineId into maGroup
        //                    from ma in maGroup.DefaultIfEmpty()
        //                    join p in _context.Plants on ma.PlantId equals p.Id into pGroup
        //                    from p in pGroup.DefaultIfEmpty()
        //                    join u in _context.Units on ma.UnitId equals u.Id into uGroup
        //                    from u in uGroup.DefaultIfEmpty()
        //                    where !ma.IsDeleted || ma == null
        //                    select new GetWashMachineDto
        //                    {
        //                        Id = m.Id,
        //                        MachineCode = m.MachineCode,
        //                        Brand = m.Brand,
        //                        Model = m.Model,
        //                        PlantName = p.Name,
        //                        UnitName = u.Name
        //                    };

        //        if (plantId.HasValue)
        //        {
        //            query = query.Where(x => x.PlantName == (from p in _context.Plants where p.Id == plantId select p.Name).FirstOrDefault());
        //        }

        //        if (unitId.HasValue)
        //        {
        //            query = query.Where(x => x.UnitName == (from u in _context.Units where u.Id == unitId select u.Name).FirstOrDefault());
        //        }

        //        var machines = await query.ToListAsync();
        //        return machines;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}



        //public async Task<MessageHelper> CreateWashPlanAsync(List<CreateWashPlanDto> data)
        //{
        //    try
        //    {
        //        DateTime bdTime = GetBdTime();
        //        var msg = new MessageHelper();

        //        foreach (var item in data)
        //        {
        //            var existing = await _context.WashPlan.FirstOrDefaultAsync(x =>
        //                x.WorkOrderId == item.WorkOrderId &&
        //                x.PlantId == item.PlantId &&
        //                x.UnitId == item.UnitId &&
        //                x.PlanDate == item.PlanDate &&
        //                x.Shift == item.Shift &&
        //                x.ProcessStageId == item.ProcessStageId
        //                );

        //            if (existing != null)
        //            {
        //                return new MessageHelper { Message = "WashPlan already exists." };
        //            }

        //            var washPlan = new WashPlan
        //            {
        //                WorkOrderId = item.WorkOrderId,
        //                ProcessStageId = item.ProcessStageId,
        //                PlanDate = item.PlanDate,
        //                Shift = item.Shift,
        //                PlantId = item.PlantId,
        //                UnitId = item.UnitId,

        //                BaseTargetQty = item.BaseTargetQty,
        //                Percentage = item.Percentage,
        //                FinalTargetQty = item.FinalTargetQty,

        //                Remarks = item.Remarks,
        //                IsDeleted = false,
        //                CreatedBy = item.CreatedBy,
        //                CreatedAt = bdTime
        //            };

        //            _context.WashPlan.Add(washPlan);
        //            await _context.SaveChangesAsync(); // get WashPlan.Id

        //            // 🔥 FIXED MACHINE INSERT
        //            if (item.MachineIds != null && item.MachineIds.Any())
        //            {
        //                var machineEntities = item.MachineIds.Select(m => new WashPlanMachine
        //                {
        //                    WashPlanId = washPlan.Id,
        //                    MachineId = m,

        //                    // IMPORTANT (missing before)
        //                    IsDeleted = false,
        //                    CreatedAt = bdTime,
        //                    CreatedBy = item.CreatedBy
        //                });

        //                _context.WashPlanMachine.AddRange(machineEntities);
        //            }
        //        }

        //        await _context.SaveChangesAsync();

        //        return new MessageHelper
        //        {
        //            Message = "Create Successfully"
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        // 🔥 REAL ERROR (VERY IMPORTANT)
        //        return new MessageHelper
        //        {
        //            Message = ex.InnerException?.Message ?? ex.Message
        //        };
        //    }
        //}

        public async Task<MessageHelper> CreateWashPlanAsync(List<CreateWashPlanDto> data)
        {
            try
            {
                DateTime bdTime = GetBdTime();

                foreach (var item in data)
                {
                    var existing = await _context.WashPlan.FirstOrDefaultAsync(x =>
                        x.WorkOrderId == item.WorkOrderId &&
                        x.PlantId == item.PlantId &&
                        x.UnitId == item.UnitId &&
                        x.PlanDate == item.PlanDate &&
                        x.Shift == item.Shift &&
                        x.ProcessStageId == item.ProcessStageId
                    );

                    if (existing != null)
                    {
                        // ✅ ONLY update FinalTargetQty
                        existing.FinalTargetQty = item.FinalTargetQty;

                        // Optional audit update
                        existing.UpdatedAt = bdTime;
                        existing.UpdatedBy = item.CreatedBy;

                        _context.WashPlan.Update(existing);
                    }
                    else
                    {
                        var washPlan = new WashPlan
                        {
                            WorkOrderId = item.WorkOrderId,
                            ProcessStageId = item.ProcessStageId,
                            PlanDate = item.PlanDate,
                            Shift = item.Shift,
                            PlantId = item.PlantId,
                            UnitId = item.UnitId,

                            BaseTargetQty = item.BaseTargetQty,
                            Percentage = item.Percentage,
                            FinalTargetQty = item.FinalTargetQty,

                            Remarks = item.Remarks,
                            IsDeleted = false,
                            CreatedBy = item.CreatedBy,
                            CreatedAt = bdTime
                        };

                        await _context.WashPlan.AddAsync(washPlan);
                        await _context.SaveChangesAsync(); // needed for Id

                        if (item.MachineIds != null && item.MachineIds.Any())
                        {
                            var machineEntities = item.MachineIds.Select(m => new WashPlanMachine
                            {
                                WashPlanId = washPlan.Id,
                                MachineId = m,
                                IsDeleted = false,
                                CreatedAt = bdTime,
                                CreatedBy = item.CreatedBy
                            });

                            await _context.WashPlanMachine.AddRangeAsync(machineEntities);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return new MessageHelper
                {
                    Message = "Create/Update Successfully"
                };
            }
            catch (Exception ex)
            {
                return new MessageHelper
                {
                    Message = ex.InnerException?.Message ?? ex.Message
                };
            }
        }

        //public async Task<List<PlantUnitDto>> GetPlantUnitListAsync()
        //{
        //    try
        //    {
        //        var result = await (from p in _context.Plants
        //                            join u in _context.Units on p.Id equals u.PlantId
        //                            select new PlantUnitDto
        //                            {
        //                                PlantId = p.Id,
        //                                PlantName = p.Name,
        //                                UnitId = u.Id,
        //                                UnitName = u.Name
        //                            }).ToListAsync();
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }
        //}

        public async Task<ApiResponse<object>> GetWashPlanAsync(WashPlanFilterDto filter)
        {
            try
            {
                // ======================
                // BASE QUERY
                // ======================
                var query = from wp in _context.WashPlan
                            join wo in _context.WorkOrders on wp.WorkOrderId equals wo.Id
                            join ps in _context.ProcessStages on wp.ProcessStageId equals ps.Id
                            join un in _context.Units on wp.UnitId equals un.Id
                            join p in _context.Plants on un.PlantId equals p.Id

                            select new WashPlanDto
                            {
                                Id = wp.Id,   // 🔥 THIS IS MISSING (CRITICAL FIX)
                                WorkOrderId = wp.WorkOrderId,
                                WorkOrderNo = wo.WorkOrderNo,
                                PlanDate = wp.PlanDate,

                                PlantId = wp.PlantId,
                                PlantName = p.Name,

                                UnitId = wp.UnitId,
                                UnitName = un.Name,

                                Shift = wp.Shift,

                                Factory = wo.Factory,
                                Line = wo.Line,

                                Buyer = wo.Buyer,
                                BuyerDepartment = wo.BuyerDepartment,
                                StyleName = wo.StyleName,
                                FastReactNo = wo.FastReactNo,
                                Color = wo.Color,
                                WashType = wo.WashType,

                                OrderQuantity = (decimal)wo.OrderQuantity,
                                CutQty = (decimal)wo.CutQty,
                                TOD = wo.TOD,

                                SewingCompDate = wo.SewingCompDate,
                                FirstRCVDate = wo.FirstRCVDate,
                                WashApprovalDate = wo.WashApprovalDate,
                                WashTargetDate = wo.WashTargetDate,

                                TotalWashReceived = (decimal)wo.TotalWashReceived,
                                TotalWashDelivery = (decimal)wo.TotalWashDelivery,
                                WashBalance = (decimal)wo.WashBalance,

                                Marks = wo.Marks,

                                ProcessStageId = wp.ProcessStageId,
                                ProcessStageName = ps.Name,

                                FinalTargetQty = wp.FinalTargetQty ?? 0,
                                BaseTargetQty = wp.BaseTargetQty ?? 0
                                
                            };

                // ======================
                // FILTERS
                // ======================
                if (filter.FromDate.HasValue)
                    query = query.Where(x => x.PlanDate >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(x => x.PlanDate <= filter.ToDate.Value);

                if (filter.PlantId.HasValue)
                    query = query.Where(x => x.PlantId == filter.PlantId.Value);

                if (filter.UnitId.HasValue)
                    query = query.Where(x => x.UnitId == filter.UnitId.Value);

                if (filter.Shift.HasValue)
                    query = query.Where(x => x.Shift == filter.Shift.Value);

                if (filter.ProcessStageId.HasValue)
                    query = query.Where(x => x.ProcessStageId == filter.ProcessStageId.Value);

                if (!string.IsNullOrEmpty(filter.Search))
                {
                    var search = filter.Search.ToLower();

                    query = query.Where(x =>
                        x.WorkOrderNo.ToLower().Contains(search) ||
                        x.Buyer.ToLower().Contains(search) ||
                        x.BuyerDepartment.ToLower().Contains(search) ||
                        x.StyleName.ToLower().Contains(search) ||
                        x.FastReactNo.ToLower().Contains(search));
                }

                // ======================
                // PAGINATION BASE DATA
                // ======================
                var totalRecords = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.PlanDate)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                // ======================
                // MACHINE ATTACHMENT (SAFE WAY)
                // ======================
                var washPlanIds = data.Select(x => x.Id).ToList();

                var machines = await _context.WashPlanMachine
                    .Where(x => washPlanIds.Contains(x.WashPlanId) && !x.IsDeleted)
                    .Join(_context.WashMachine,
                        wp => wp.MachineId,
                        wm => wm.Id,
                        (wp, wm) => new
                        {
                            wp.WashPlanId,
                            wm.Id,
                            wm.MachineCode
                        })
                    .ToListAsync();

                foreach (var item in data)
                {
                    item.Machines = machines
                        .Where(x => x.WashPlanId == item.Id)
                        .Select(x => new MachineDto
                        {
                            MachineId = x.Id,
                            MachineCode = x.MachineCode
                        })
                        .ToList();
                }

                // ======================
                // RESPONSE
                // ======================
                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Wash plan fetched successfully",
                    Data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize,
                        Records = data
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResponse<object>> GetWashPlanModalAsync(WashPlanModalFilterDto filter)
        {
            try
            {
                var query = (from wo in _context.WorkOrders 
                            join u in _context.Units on wo.Unit equals u.Name
                            join p in _context.Plants on u.PlantId equals p.Id

                             select new GetWashPlanDto
                             {
                                 Plant = p.Name,

                                 Unit = wo.Unit,
                                 Buyer = wo.Buyer,
                                 StyleName = wo.StyleName,
                                 FastReactNo = wo.FastReactNo,
                                 Color = wo.Color,

                                 WorkOrderId = wo.Id,
                                 WorkOrderNo = wo.WorkOrderNo,

                                 OrderQuantity = wo.OrderQuantity ?? 0,
                                 TOD = wo.TOD,

                                 WashBalance = wo.WashBalance ?? 0,
                                 FromReceived = wo.FromReceived ?? 0,

                                 Marks = wo.Marks,

                                 FirstWashBatchQty = wo.FirstWashBatchQty,
                                 FirstWashBatchTime = wo.FirstWashBatchTime,

                                 SecondWashBatchQty = wo.SecondWashBatchQty,
                                 SecondWashBatchTime = wo.SecondWashBatchTime
                             });

                // ======================
                // SEARCH (optional)
                // ======================
                if (!string.IsNullOrEmpty(filter.Search))
                {
                    var search = filter.Search.ToLower();

                    query = query.Where(x =>
                        x.WorkOrderNo.ToLower().Contains(search) ||
                        x.Buyer.ToLower().Contains(search) ||
                        x.StyleName.ToLower().Contains(search) ||
                        x.FastReactNo.ToLower().Contains(search)
                    );
                }

                // ======================
                // PAGINATION
                // ======================
                var totalRecords = await query.CountAsync();

                var data = await query
                    .OrderByDescending(x => x.WorkOrderNo)
                    .Skip((filter.PageNumber - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                return new ApiResponse<object>
                {
                    Success = true,
                    Message = "Wash plan modal data fetched successfully",
                    Data = new
                    {
                        TotalRecords = totalRecords,
                        PageNumber = filter.PageNumber,
                        PageSize = filter.PageSize,
                        Records = data
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

    }
}
