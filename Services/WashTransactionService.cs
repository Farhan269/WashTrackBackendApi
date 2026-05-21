using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Extensions;
using wsahRecieveDelivary.Helpers;
using wsahRecieveDelivary.Models;
using wsahRecieveDelivary.Models.Enums;

namespace wsahRecieveDelivary.Services
{
    public class WashTransactionService : IWashTransactionService
    {
        private readonly ApplicationDbContext _context;

        public WashTransactionService(ApplicationDbContext context)
        {
            _context = context;
        }
        private DateTime GetBdTime()
        {
            //TimeZoneInfo bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            //return TimeZoneInfo.ConvertTime(DateTime.UtcNow, bdTimeZone);
            TimeZoneInfo bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Bangladesh Standard Time");
            DateTime bdTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, bdTimeZone);

            // ADD THIS LINE: Tell .NET to treat this as UTC so the database doesn't shift it
            return DateTime.SpecifyKind(bdTime, DateTimeKind.Utc);
        }
        private DateTime BdTime => GetBdTime();

     

        //public async Task<WashTransactionResponseDto> CreateReceiveAsync(CreateWashTransactionDto dto, int userId)
        //{
        //    try
        //    {
        //        Console.WriteLine("➕ CreateReceiveAsync called");

        //        var workOrder = await _context.WorkOrders.FindAsync(dto.WorkOrderId);
        //        if (workOrder == null)
        //            throw new KeyNotFoundException($"WorkOrder with ID {dto.WorkOrderId} not found");

        //        var processStage = await _context.ProcessStages.FindAsync(dto.ProcessStageId);
        //        if (processStage == null)
        //            throw new KeyNotFoundException($"ProcessStage with ID {dto.ProcessStageId} not found");

        //        // ✅ FIXED: Get BD time once
        //        DateTime bdTime = GetBdTime();
        //        Console.WriteLine($"   BD Time: {bdTime:yyyy-MM-dd HH:mm:ss.fffffff}");

        //        var transaction = new WashTransaction
        //        {
        //            WorkOrderId = dto.WorkOrderId,
        //            TransactionType = TransactionType.Receive,
        //            ProcessStageId = dto.ProcessStageId,
        //            Quantity = dto.Quantity,
        //            // ✅ FIXED: TransactionDate = CreatedAt (same BD time)
        //            TransactionDate = bdTime,
        //            BatchNo = dto.BatchNo,
        //            GatePassNo = dto.GatePassNo,
        //            Remarks = dto.Remarks,
        //            ReceivedBy = dto.ReceivedBy,
        //            DeliveredTo = dto.DeliveredTo,
        //            CreatedBy = userId,
        //            CreatedAt = bdTime,
        //            IsActive = true
        //        };

        //        _context.WashTransactions.Add(transaction);
        //        await _context.SaveChangesAsync();

        //        Console.WriteLine($"✅ Receive transaction created");
        //        Console.WriteLine($"   TransactionDate = CreatedAt: {transaction.TransactionDate}");

        //        await UpdateStageBalanceAsync(dto.WorkOrderId, dto.ProcessStageId);

        //        return await GetByIdAsync(transaction.Id)
        //            ?? throw new Exception("Failed to retrieve created transaction");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ CreateReceive error: {ex.Message}");
        //        throw;
        //    }
        //}



        //Farhan Mashuq--- New CreateReceive

        public async Task<WashTransactionResponseDto> CreateReceiveAsync(CreateWashTransactionDto dto, int userId)
        {
            try
            {
                Console.WriteLine("➕ CreateReceiveAsync called");

                var workOrder = await _context.WorkOrders.FindAsync(dto.WorkOrderId);
                if (workOrder == null)
                    throw new KeyNotFoundException($"WorkOrder with ID {dto.WorkOrderId} not found");

                var processStage = await _context.ProcessStages.FindAsync(dto.ProcessStageId);
                if (processStage == null)
                    throw new KeyNotFoundException($"ProcessStage with ID {dto.ProcessStageId} not found");

                 DateTime bdTime = GetBdTime();

                var transactionDate = bdTime;

                Console.WriteLine($"   Transaction Date (UTC): {transactionDate:yyyy-MM-dd HH:mm:ss.fff}");

               // var bdTime = DateTimeHelper.GetBangladeshTimeFromUtc(transactionDate);
                Console.WriteLine($"   Transaction Date (BD Time): {bdTime:yyyy-MM-dd HH:mm:ss.fff}");

                var activeSchedule = await GetActiveScheduleAsync(transactionDate);
                if (activeSchedule == null)
                    throw new InvalidOperationException("No active shift schedule found for this date");

                //Console.WriteLine($"   Active Schedule: {activeSchedule.Name}");
                //Console.WriteLine($"   Day Shift: {activeSchedule.DayShiftStart} - {activeSchedule.DayShiftEnd}");
                //Console.WriteLine($"   Night Shift: {activeSchedule.NightShiftStart} - {activeSchedule.NightShiftEnd}");

                var (shiftDate, shiftType, shiftScheduleId) = ShiftHelper.CalculateShift(transactionDate, activeSchedule);

                //Console.WriteLine($"   Calculated Shift Date: {shiftDate:yyyy-MM-dd}");
                //Console.WriteLine($"   Calculated Shift Type: {shiftType}");
                //Console.WriteLine($"   Shift Schedule ID: {shiftScheduleId}");

                var transaction = new WashTransaction
                {
                    WorkOrderId = dto.WorkOrderId,
                    TransactionType = TransactionType.Receive,
                    ProcessStageId = dto.ProcessStageId,
                    Quantity = dto.Quantity,
                    TransactionDate = transactionDate,
                    BatchNo = dto.BatchNo,
                    GatePassNo = dto.GatePassNo,
                    Remarks = dto.Remarks,
                    ReceivedBy = dto.ReceivedBy,
                    CreatedBy = userId,
                    CreatedAt = bdTime,

                    ShiftDate = shiftDate,
                    ShiftType = shiftType,
                    ShiftScheduleId = shiftScheduleId
                };

                _context.WashTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                Console.WriteLine("✅ Receive transaction created successfully");

                await UpdateStageBalanceAsync(dto.WorkOrderId, dto.ProcessStageId);

                return await GetByIdAsync(transaction.Id)
                   ?? throw new Exception("Failed to retrieve created transaction");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CreateReceive error: {ex.Message}");
                throw;
            }
        }

        //public async Task<WashTransactionResponseDto> CreateDeliveryAsync(CreateWashTransactionDto dto, int userId)
        //{
        //    try
        //    {
        //        Console.WriteLine("➕ CreateDeliveryAsync called");

        //        var workOrder = await _context.WorkOrders.FindAsync(dto.WorkOrderId);
        //        if (workOrder == null)
        //            throw new KeyNotFoundException($"WorkOrder with ID {dto.WorkOrderId} not found");

        //        var processStage = await _context.ProcessStages.FindAsync(dto.ProcessStageId);
        //        if (processStage == null)
        //            throw new KeyNotFoundException($"ProcessStage with ID {dto.ProcessStageId} not found");

        //        // ✅ FIXED: Get BD time once
        //        DateTime bdTime = GetBdTime();
        //        Console.WriteLine($"   BD Time: {bdTime:yyyy-MM-dd HH:mm:ss.fffffff}");

        //        var transaction = new WashTransaction
        //        {
        //            WorkOrderId = dto.WorkOrderId,
        //            TransactionType = TransactionType.Delivery,
        //            ProcessStageId = dto.ProcessStageId,
        //            Quantity = dto.Quantity,
        //            // ✅ FIXED: TransactionDate = CreatedAt (same BD time)
        //            TransactionDate = bdTime,
        //            BatchNo = dto.BatchNo,
        //            GatePassNo = dto.GatePassNo,
        //            Remarks = dto.Remarks,
        //            ReceivedBy = dto.ReceivedBy,
        //            DeliveredTo = dto.DeliveredTo,
        //            CreatedBy = userId,
        //            CreatedAt = bdTime,
        //            IsActive = true
        //        };

        //        _context.WashTransactions.Add(transaction);
        //        await _context.SaveChangesAsync();

        //        Console.WriteLine($"✅ Delivery transaction created");
        //        Console.WriteLine($"   TransactionDate = CreatedAt: {transaction.TransactionDate}");

        //        await UpdateStageBalanceAsync(dto.WorkOrderId, dto.ProcessStageId);

        //        return await GetByIdAsync(transaction.Id)
        //            ?? throw new Exception("Failed to retrieve created transaction");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ CreateDelivery error: {ex.Message}");
        //        throw;
        //    }
        //}




        public async Task<WashTransactionResponseDto> CreateDeliveryAsync(CreateWashTransactionDto dto, int userId)
        {
            try
            {
                Console.WriteLine("➕ CreateDeliveryAsync called");

                var workOrder = await _context.WorkOrders.FindAsync(dto.WorkOrderId);
                if (workOrder == null)
                    throw new KeyNotFoundException($"WorkOrder with ID {dto.WorkOrderId} not found");

                var processStage = await _context.ProcessStages.FindAsync(dto.ProcessStageId);
                if (processStage == null)
                    throw new KeyNotFoundException($"ProcessStage with ID {dto.ProcessStageId} not found");

                DateTime bdTime = GetBdTime();

                var transactionDate = bdTime;

                Console.WriteLine($"   Transaction Date (UTC): {transactionDate:yyyy-MM-dd HH:mm:ss.fff}");

                //var bdTime = DateTimeHelper.GetBangladeshTimeFromUtc(transactionDate);
                Console.WriteLine($"   Transaction Date (BD Time): {bdTime:yyyy-MM-dd HH:mm:ss.fff}");

                var activeSchedule = await GetActiveScheduleAsync(transactionDate);
                if (activeSchedule == null)
                    throw new InvalidOperationException("No active shift schedule found for this date");

                //Console.WriteLine($"   Active Schedule: {activeSchedule.Name}");
                //Console.WriteLine($"   Day Shift: {activeSchedule.DayShiftStart} - {activeSchedule.DayShiftEnd}");
                //Console.WriteLine($"   Night Shift: {activeSchedule.NightShiftStart} - {activeSchedule.NightShiftEnd}");

                var (shiftDate, shiftType, shiftScheduleId) = ShiftHelper.CalculateShift(transactionDate, activeSchedule);

                //Console.WriteLine($"   Calculated Shift Date: {shiftDate:yyyy-MM-dd}");
                //Console.WriteLine($"   Calculated Shift Type: {shiftType}");
                //Console.WriteLine($"   Shift Schedule ID: {shiftScheduleId}");

                var transaction = new WashTransaction
                {
                    WorkOrderId = dto.WorkOrderId,
                    TransactionType = TransactionType.Delivery,
                    ProcessStageId = dto.ProcessStageId,
                    Quantity = dto.Quantity,
                    TransactionDate = transactionDate,
                    BatchNo = dto.BatchNo,
                    GatePassNo = dto.GatePassNo,
                    Remarks = dto.Remarks,
                    DeliveredTo = dto.DeliveredTo,
                    CreatedBy = userId,
                    CreatedAt = bdTime,

                    ShiftDate = shiftDate,
                    ShiftType = shiftType,
                    ShiftScheduleId = shiftScheduleId
                };

                _context.WashTransactions.Add(transaction);
                await _context.SaveChangesAsync();

                //Console.WriteLine("✅ Delivery transaction created successfully");
                //Console.WriteLine($"   Saved ShiftDate: {transaction.ShiftDate:yyyy-MM-dd}");
                //Console.WriteLine($"   Saved ShiftType: {transaction.ShiftType}");
                //Console.WriteLine($"   Saved TransactionDate: {transaction.TransactionDate:yyyy-MM-dd HH:mm:ss.fff}");
                await UpdateStageBalanceAsync(dto.WorkOrderId, dto.ProcessStageId);

                return await GetByIdAsync(transaction.Id)
                   ?? throw new Exception("Failed to retrieve created transaction");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in CreateDeliveryAsync: {ex.Message}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
                throw;
            }
        }


        public async Task<WashTransactionResponseDto> UpdateAsync(int id, CreateWashTransactionDto dto, int userId)
        {
            try
            {
                Console.WriteLine($"✏️ UpdateAsync called for transaction {id}");

                var transaction = await _context.WashTransactions.FindAsync(id);
                if (transaction == null || !transaction.IsActive)
                    throw new KeyNotFoundException($"Transaction with ID {id} not found");

                var oldStageId = transaction.ProcessStageId;

                // ✅ FIXED: Don't change TransactionDate on update
                // Update fields
                transaction.TransactionType = dto.TransactionType;
                transaction.ProcessStageId = dto.ProcessStageId;
                transaction.Quantity = dto.Quantity;
                transaction.BatchNo = dto.BatchNo;
                transaction.GatePassNo = dto.GatePassNo;
                transaction.Remarks = dto.Remarks;
                transaction.ReceivedBy = dto.ReceivedBy;
                transaction.DeliveredTo = dto.DeliveredTo;
                transaction.UpdatedBy = userId;
                transaction.UpdatedAt = GetBdTime();

                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Transaction updated");
                Console.WriteLine($"   TransactionDate unchanged: {transaction.TransactionDate}");
                Console.WriteLine($"   UpdatedAt: {transaction.UpdatedAt}");

                // Update balances
                if (oldStageId != transaction.ProcessStageId)
                {
                    await UpdateStageBalanceAsync(transaction.WorkOrderId, oldStageId);
                    await UpdateStageBalanceAsync(transaction.WorkOrderId, transaction.ProcessStageId);
                }
                else
                {
                    await UpdateStageBalanceAsync(transaction.WorkOrderId, oldStageId);
                }

                return await GetByIdAsync(transaction.Id)
                    ?? throw new Exception("Failed to retrieve updated transaction");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UpdateAsync error: {ex.Message}");
                throw;
            }
        }





        // ==========================================
        // DELETE (SOFT DELETE)
        // ==========================================
        // ==========================================
        // DELETE (SOFT DELETE) - FIXED
        // ==========================================
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                Console.WriteLine($"🗑️ DeleteAsync called for transaction {id}");

                var transaction = await _context.WashTransactions.FindAsync(id);
                if (transaction == null || !transaction.IsActive)
                    return false;

                transaction.IsActive = false;
                transaction.UpdatedAt = GetBdTime(); // ✅ FIXED: Use GetBdTime()

                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Transaction soft deleted");

                await UpdateStageBalanceAsync(transaction.WorkOrderId, transaction.ProcessStageId);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ DeleteAsync error: {ex.Message}");
                throw;
            }
        }

        // ==========================================
        // PRIVATE HELPER: UPDATE BALANCE
        // ==========================================
        // ==========================================
        // PRIVATE HELPER: UPDATE BALANCE
        // ==========================================
        private async Task UpdateStageBalanceAsync(int workOrderId, int processStageId)
        {
            try
            {
                var balance = await _context.ProcessStageBalances
                    .FirstOrDefaultAsync(b => b.WorkOrderId == workOrderId && b.ProcessStageId == processStageId);

                if (balance == null)
                {
                    balance = new ProcessStageBalance
                    {
                        WorkOrderId = workOrderId,
                        ProcessStageId = processStageId
                    };
                    _context.ProcessStageBalances.Add(balance);
                }

                var transactions = await _context.WashTransactions
                    .Where(t => t.WorkOrderId == workOrderId && t.ProcessStageId == processStageId && t.IsActive)
                    .ToListAsync();

                balance.TotalReceived = (int)transactions
                    .Where(t => t.TransactionType == TransactionType.Receive)
                    .Sum(t => t.Quantity);

                balance.TotalDelivered = (int)transactions
                    .Where(t => t.TransactionType == TransactionType.Delivery)
                    .Sum(t => t.Quantity);

                balance.CurrentBalance = balance.TotalReceived - balance.TotalDelivered;

                balance.LastReceiveDate = transactions
                    .Where(t => t.TransactionType == TransactionType.Receive)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => (DateTime?)t.TransactionDate)
                    .FirstOrDefault();

                balance.LastDeliveryDate = transactions
                    .Where(t => t.TransactionType == TransactionType.Delivery)
                    .OrderByDescending(t => t.TransactionDate)
                    .Select(t => (DateTime?)t.TransactionDate)
                    .FirstOrDefault();

                // ✅ FIXED: Use GetBdTime()
                balance.LastUpdated = GetBdTime();

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ UpdateStageBalanceAsync error: {ex.Message}");
                throw;
            }
        }

        // ==========================================
        // GET BY ID
        // ==========================================
        public async Task<WashTransactionResponseDto?> GetByIdAsync(int id)
        {
            return await _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Include(t => t.UpdatedByUser)
                .Where(t => t.Id == id && t.IsActive)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    ShiftDate = t.ShiftDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt,
                    UpdatedByUsername = t.UpdatedByUser != null ? t.UpdatedByUser.Username : null,
                    UpdatedAt = t.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        // ==========================================
        // GET ALL
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetAllAsync()
        {
            return await _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET BY WORK ORDER
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetByWorkOrderAsync(int workOrderId)
        {
            return await _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.WorkOrderId == workOrderId && t.IsActive)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET BY STAGE
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetByStageAsync(int processStageId)
        {
            return await _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.ProcessStageId == processStageId && t.IsActive)
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET BY FILTER
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetByFilterAsync(WashTransactionFilterDto filter)
        {
            var query = _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.IsActive);

            if (filter.WorkOrderId.HasValue)
                query = query.Where(t => t.WorkOrderId == filter.WorkOrderId.Value);

            if (filter.TransactionType.HasValue)
                query = query.Where(t => t.TransactionType == filter.TransactionType.Value);

            if (filter.ProcessStageId.HasValue)
                query = query.Where(t => t.ProcessStageId == filter.ProcessStageId.Value);

            if (filter.StartDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date >= filter.StartDate.Value.Date);

            if (filter.EndDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date <= filter.EndDate.Value.Date);

            if (!string.IsNullOrEmpty(filter.BatchNo))
                query = query.Where(t => t.BatchNo == filter.BatchNo);

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET BALANCES BY WORK ORDER
        // ==========================================
        public async Task<List<ProcessBalanceDto>> GetBalancesByWorkOrderAsync(int workOrderId)
        {
            var workOrder = await _context.WorkOrders.FindAsync(workOrderId);
            if (workOrder == null)
                throw new KeyNotFoundException($"WorkOrder with ID {workOrderId} not found");

            var balances = await _context.ProcessStageBalances
                .Include(b => b.ProcessStage)
                .Where(b => b.WorkOrderId == workOrderId)
                .ToListAsync();

            return balances.Select(b => new ProcessBalanceDto
            {
                WorkOrderId = workOrder.Id,
                WorkOrderNo = workOrder.WorkOrderNo,
                StyleName = workOrder.StyleName,
                ProcessStageId = b.ProcessStageId,
                ProcessStageName = b.ProcessStage.Name,
                TotalReceived = b.TotalReceived,
                TotalDelivered = b.TotalDelivered,
                CurrentBalance = b.CurrentBalance,
                LastReceiveDate = b.LastReceiveDate,
                LastDeliveryDate = b.LastDeliveryDate
            }).ToList();
        }

        // ==========================================
        // GET WASH STATUS
        // ==========================================
        public async Task<WorkOrderWashStatusDto?> GetWashStatusAsync(int workOrderId)
        {
            var workOrder = await _context.WorkOrders.FindAsync(workOrderId);
            if (workOrder == null) return null;

            var balances = await _context.ProcessStageBalances
                .Include(b => b.ProcessStage)
                .Where(b => b.WorkOrderId == workOrderId)
                .OrderBy(b => b.ProcessStage.DisplayOrder)
                .ToListAsync();

            var result = new WorkOrderWashStatusDto
            {
                WorkOrderId = workOrder.Id,
                WorkOrderNo = workOrder.WorkOrderNo,
                StyleName = workOrder.StyleName,
                Buyer = workOrder.Buyer,
                Factory = workOrder.Factory,
                Line = workOrder.Line,
                WashType = workOrder.WashType,
                // ✅ FIX: Convert int? to int
                OrderQuantity = workOrder.OrderQuantity.GetValueOrDefault(),
                StageBalances = new Dictionary<string, ProcessBalanceDto>()
            };

            foreach (var balance in balances)
            {
                var balanceDto = new ProcessBalanceDto
                {
                    WorkOrderId = workOrder.Id,
                    WorkOrderNo = workOrder.WorkOrderNo,
                    StyleName = workOrder.StyleName,
                    ProcessStageId = balance.ProcessStageId,
                    ProcessStageName = balance.ProcessStage.Name,
                    TotalReceived = balance.TotalReceived,
                    TotalDelivered = balance.TotalDelivered,
                    CurrentBalance = balance.CurrentBalance,
                    LastReceiveDate = balance.LastReceiveDate,
                    LastDeliveryDate = balance.LastDeliveryDate
                };

                result.StageBalances[balance.ProcessStage.Name] = balanceDto;
            }

            result.TotalReceived = balances.Sum(b => b.TotalReceived);
            result.TotalDelivered = balances.Sum(b => b.TotalDelivered);
            result.OverallBalance = balances.Sum(b => b.CurrentBalance);

            // ✅ FIX: Safely handle nullable int for calculation
            int orderQty = workOrder.OrderQuantity.GetValueOrDefault();
            if (orderQty > 0)
            {
                result.CompletionPercentage = Math.Round((decimal)result.TotalDelivered / orderQty * 100, 2);
            }

            return result;
        }

        // ==========================================
        // GET ALL WASH STATUSES (OPTIMIZED - No N+1)
        // ==========================================
        public async Task<List<WorkOrderWashStatusDto>> GetAllWashStatusesAsync()
        {
            var workOrders = await _context.WorkOrders.ToListAsync();

            // Get all balances with related data in ONE query
            var allBalances = await _context.ProcessStageBalances
                .Include(b => b.ProcessStage)
                .ToListAsync();

            var results = new List<WorkOrderWashStatusDto>();

            foreach (var workOrder in workOrders)
            {
                // Filter balances for this workorder (in-memory operation)
                var workOrderBalances = allBalances
                    .Where(b => b.WorkOrderId == workOrder.Id)
                    .OrderBy(b => b.ProcessStage.DisplayOrder)
                    .ToList();

                var result = new WorkOrderWashStatusDto
                {
                    WorkOrderId = workOrder.Id,
                    WorkOrderNo = workOrder.WorkOrderNo,
                    StyleName = workOrder.StyleName,
                    Buyer = workOrder.Buyer,
                    Factory = workOrder.Factory,
                    Line = workOrder.Line,
                    WashType = workOrder.WashType,
                    // ✅ FIX: Convert int? to int
                    OrderQuantity = workOrder.OrderQuantity.GetValueOrDefault(),
                    StageBalances = new Dictionary<string, ProcessBalanceDto>()
                };

                // Build stage balances dictionary
                foreach (var balance in workOrderBalances)
                {
                    var balanceDto = new ProcessBalanceDto
                    {
                        WorkOrderId = workOrder.Id,
                        WorkOrderNo = workOrder.WorkOrderNo,
                        StyleName = workOrder.StyleName,
                        ProcessStageId = balance.ProcessStageId,
                        ProcessStageName = balance.ProcessStage.Name,
                        TotalReceived = balance.TotalReceived,
                        TotalDelivered = balance.TotalDelivered,
                        CurrentBalance = balance.CurrentBalance,
                        LastReceiveDate = balance.LastReceiveDate,
                        LastDeliveryDate = balance.LastDeliveryDate
                    };

                    result.StageBalances[balance.ProcessStage.Name] = balanceDto;
                }

                // Calculate overall summary
                result.TotalReceived = workOrderBalances.Sum(b => b.TotalReceived);
                result.TotalDelivered = workOrderBalances.Sum(b => b.TotalDelivered);
                result.OverallBalance = workOrderBalances.Sum(b => b.CurrentBalance);

                // ✅ FIX: Safely handle nullable int for calculation
                int orderQty = workOrder.OrderQuantity.GetValueOrDefault();
                if (orderQty > 0)
                {
                    result.CompletionPercentage = Math.Round(
                        (decimal)result.TotalDelivered / orderQty * 100,
                        2);
                }

                results.Add(result);
            }

            return results;
        }
   

        // ==========================================
        // GET STAGE SUMMARY
        // ==========================================
        public async Task<List<ProcessStageSummaryDto>> GetStageSummaryAsync()
        {
            var allStages = await _context.ProcessStages
                .Where(ps => ps.IsActive)
                .OrderBy(ps => ps.DisplayOrder)
                .ToListAsync();

            var summaries = new List<ProcessStageSummaryDto>();

            foreach (var stage in allStages)
            {
                var transactions = await _context.WashTransactions
                    .Where(t => t.ProcessStageId == stage.Id && t.IsActive)
                    .ToListAsync();

                var summary = new ProcessStageSummaryDto
                {
                    ProcessStageId = stage.Id,
                    ProcessStageName = stage.Name,
                    TotalReceiveCount = transactions.Count(t => t.TransactionType == TransactionType.Receive),
                    TotalDeliveryCount = transactions.Count(t => t.TransactionType == TransactionType.Delivery),
                    TotalReceivedQty = transactions.Where(t => t.TransactionType == TransactionType.Receive).Sum(t => t.Quantity),
                    TotalDeliveredQty = transactions.Where(t => t.TransactionType == TransactionType.Delivery).Sum(t => t.Quantity),
                    CurrentBalance = transactions.Where(t => t.TransactionType == TransactionType.Receive).Sum(t => t.Quantity) -
                                   transactions.Where(t => t.TransactionType == TransactionType.Delivery).Sum(t => t.Quantity)
                };

                summaries.Add(summary);
            }

            return summaries;
        }

        // ==========================================
        // GET RECEIVES BY STAGE
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetReceivesByStageAsync(int processStageId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.ProcessStageId == processStageId && t.TransactionType == TransactionType.Receive && t.IsActive);

            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date <= endDate.Value.Date);

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // GET DELIVERIES BY STAGE
        // ==========================================
        public async Task<List<WashTransactionResponseDto>> GetDeliveriesByStageAsync(int processStageId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.WashTransactions
                .Include(t => t.WorkOrder)
                .Include(t => t.ProcessStage)
                .Include(t => t.CreatedByUser)
                .Where(t => t.ProcessStageId == processStageId && t.TransactionType == TransactionType.Delivery && t.IsActive);

            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate.Date <= endDate.Value.Date);

            return await query
                .OrderByDescending(t => t.TransactionDate)
                .Select(t => new WashTransactionResponseDto
                {
                    Id = t.Id,
                    WorkOrderId = t.WorkOrderId,
                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                    StyleName = t.WorkOrder.StyleName,
                    Buyer = t.WorkOrder.Buyer,
                    Factory = t.WorkOrder.Factory,
                    Line = t.WorkOrder.Line,
                    TransactionType = t.TransactionType,
                    TransactionTypeName = t.TransactionType.ToString(),
                    ProcessStageId = t.ProcessStageId,
                    ProcessStageName = t.ProcessStage.Name,
                    Quantity = t.Quantity,
                    TransactionDate = t.TransactionDate,
                    BatchNo = t.BatchNo,
                    GatePassNo = t.GatePassNo,
                    Remarks = t.Remarks,
                    ReceivedBy = t.ReceivedBy,
                    DeliveredTo = t.DeliveredTo,
                    CreatedBy = t.CreatedBy,
                    CreatedByUsername = t.CreatedByUser.Username,
                    CreatedAt = t.CreatedAt
                })
                .ToListAsync();
        }

        // ==========================================
        // PRIVATE HELPER: GET BALANCE
        // ==========================================
        private async Task<int> GetCurrentBalanceAsync(int workOrderId, int processStageId)
        {
            var balance = await _context.ProcessStageBalances
                .FirstOrDefaultAsync(b => b.WorkOrderId == workOrderId && b.ProcessStageId == processStageId);

            return balance?.CurrentBalance ?? 0;
        }

        // ==========================================
        // GET PAGINATED WITH FAST SEARCH & FILTERS
        // ==========================================
        // ==========================================
        // GET PAGINATED WITH FAST SEARCH & FILTERS (UPDATED)
        // ==========================================
        // ==========================================
        // GET PAGINATED WITH FAST SEARCH & FILTERS (FIXED)
        // ==========================================
        public async Task<PaginatedResponseDto<WashTransactionResponseDto>> GetPaginatedAsync(
            TransactionPaginationRequestDto request)
        {
            try
            {
                Console.WriteLine("📄 GetPaginatedAsync called");
                Console.WriteLine($"   Page: {request.Page}");
                Console.WriteLine($"   PageSize: {request.PageSize}");
                Console.WriteLine($"   SearchTerm: {request.SearchTerm}");
                Console.WriteLine($"   Buyer: {request.Buyer}");
                Console.WriteLine($"   Factory: {request.Factory}");
                Console.WriteLine($"   Unit: {request.Unit}"); // ✅ ADDED
                Console.WriteLine($"   ProcessStageId: {request.ProcessStageId}");
                Console.WriteLine($"   TransactionTypeId: {request.TransactionTypeId}");
                Console.WriteLine($"   StartDate: {request.StartDate:yyyy-MM-dd}");
                Console.WriteLine($"   EndDate: {request.EndDate:yyyy-MM-dd}");
                Console.WriteLine($"   SortBy: {request.SortBy}");
                Console.WriteLine($"   SortOrder: {request.SortOrder}");

                // Build query with AsNoTracking for read-only performance
                var query = _context.WashTransactions
                    .AsNoTracking()
                    .Include(t => t.WorkOrder)
                    .Include(t => t.ProcessStage)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.UpdatedByUser)
                    .Where(t => t.IsActive)
                    .AsQueryable();

                Console.WriteLine($"📊 Initial query count (before filters): {query.Count()}");

                // Apply global search
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    Console.WriteLine($"🔍 Applying search: {request.SearchTerm}");
                    query = query.SearchTransaction(request.SearchTerm);
                    Console.WriteLine($"   After search: {query.Count()} records");
                }

                // ✅ FIXED: Apply advanced filters with all parameters
                if (!string.IsNullOrEmpty(request.Buyer) ||
                    !string.IsNullOrEmpty(request.Factory) ||
                    !string.IsNullOrEmpty(request.Unit) || // ✅ ADDED
                    request.ProcessStageId.HasValue ||
                    request.TransactionTypeId.HasValue ||
                    request.StartDate.HasValue ||
                    request.EndDate.HasValue)
                {
                    Console.WriteLine("🎛️ Applying filters...");

                    if (!string.IsNullOrEmpty(request.Buyer))
                    {
                        Console.WriteLine($"   Filter Buyer: {request.Buyer}");
                        query = query.Where(t => t.WorkOrder.Buyer.ToLower().Contains(request.Buyer.ToLower()));
                    }

                    if (!string.IsNullOrEmpty(request.Factory))
                    {
                        Console.WriteLine($"   Filter Factory: {request.Factory}");
                        query = query.Where(t => t.WorkOrder.Factory.ToLower() == request.Factory.ToLower());
                    }

                    if (!string.IsNullOrEmpty(request.Unit)) // ✅ ADDED
                    {
                        Console.WriteLine($"   Filter Unit: {request.Unit}");
                        query = query.Where(t => t.WorkOrder.Unit.ToLower() == request.Unit.ToLower());
                    }

                    if (request.ProcessStageId.HasValue)
                    {
                        Console.WriteLine($"   Filter ProcessStageId: {request.ProcessStageId}");
                        query = query.Where(t => t.ProcessStageId == request.ProcessStageId.Value);
                    }

                    if (request.TransactionTypeId.HasValue)
                    {
                        Console.WriteLine($"   Filter TransactionTypeId: {request.TransactionTypeId}");
                        query = query.Where(t => (int)t.TransactionType == request.TransactionTypeId.Value);
                    }

                    // ✅ CRITICAL: Date filtering
                    if (request.StartDate.HasValue)
                    {
                        Console.WriteLine($"   Filter StartDate: {request.StartDate:yyyy-MM-dd}");
                        var startDateOnly = request.StartDate.Value.Date;
                        query = query.Where(t => t.TransactionDate.Date >= startDateOnly);
                        Console.WriteLine($"   After StartDate filter: {query.Count()} records");
                    }

                    if (request.EndDate.HasValue)
                    {
                        Console.WriteLine($"   Filter EndDate: {request.EndDate:yyyy-MM-dd}");
                        var endDateOnly = request.EndDate.Value.Date;
                        query = query.Where(t => t.TransactionDate.Date <= endDateOnly);
                        Console.WriteLine($"   After EndDate filter: {query.Count()} records");
                    }

                    Console.WriteLine($"   After all filters: {query.Count()} records");
                }

                // Apply sorting
                Console.WriteLine($"📊 Applying sort: {request.SortBy} ({request.SortOrder})");
                query = query.ApplyTransactionSort(request.SortBy, request.SortOrder);

                // Get total count BEFORE pagination
                var totalCount = await query.CountAsync();
                Console.WriteLine($"📊 Total count after all filters: {totalCount}");

                // Calculate total pages
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                Console.WriteLine($"📄 Total pages: {totalPages}");

                // Apply pagination
                var skip = (request.Page - 1) * request.PageSize;
                Console.WriteLine($"📄 Skip: {skip}, Take: {request.PageSize}");

                var data = await query
                    .Skip(skip)
                    .Take(request.PageSize)
                    .Select(t => new WashTransactionResponseDto
                    {
                        Id = t.Id,
                        WorkOrderId = t.WorkOrderId,
                        WorkOrderNo = t.WorkOrder.WorkOrderNo,
                        StyleName = t.WorkOrder.StyleName,
                        Buyer = t.WorkOrder.Buyer,
                        Factory = t.WorkOrder.Factory,
                        Line = t.WorkOrder.Line,
                        TransactionType = t.TransactionType,
                        TransactionTypeName = t.TransactionType.ToString(),
                        ProcessStageId = t.ProcessStageId,
                        ProcessStageName = t.ProcessStage.Name,
                        Quantity = t.Quantity,
                        TransactionDate = t.TransactionDate,
                        BatchNo = t.BatchNo,
                        GatePassNo = t.GatePassNo,
                        Remarks = t.Remarks,
                        ReceivedBy = t.ReceivedBy,
                        DeliveredTo = t.DeliveredTo,
                        CreatedBy = t.CreatedBy,
                        CreatedByUsername = t.CreatedByUser.Username,
                        CreatedAt = t.CreatedAt,
                        UpdatedByUsername = t.UpdatedByUser != null ? t.UpdatedByUser.Username : null,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                Console.WriteLine($"✅ Loaded {data.Count} records for page {request.Page}");

                return new PaginatedResponseDto<WashTransactionResponseDto>
                {
                    Success = true,
                    Message = totalCount == 0 ? "No transactions found" : null,
                    Data = data,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = request.Page,
                        PageSize = request.PageSize,
                        TotalRecords = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = request.Page > 1,
                        HasNext = request.Page < totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GetPaginatedAsync Error: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");

                return new PaginatedResponseDto<WashTransactionResponseDto>
                {
                    Success = false,
                    Message = $"Error retrieving transactions: {ex.Message}",
                    Data = new List<WashTransactionResponseDto>(),
                    Pagination = new PaginationMetadata()
                };
            }
        }



        // ==========================================
        // GET USER TRANSACTIONS SUMMARY (UNIFIED)
        // ==========================================
        public async Task<UserTransactionSummaryDto> GetUserTransactionsSummaryAsync(
            int userId,
            TransactionPaginationRequestDto request)
        {
            try
            {
                Console.WriteLine($"👤 GetUserTransactionsSummaryAsync called");
                Console.WriteLine($"   UserId: {userId}");
                Console.WriteLine($"   Page: {request.Page}, PageSize: {request.PageSize}");
                Console.WriteLine($"   StartDate: {request.StartDate}");
                Console.WriteLine($"   EndDate: {request.EndDate}");
                Console.WriteLine($"   IncludeDayWiseBreakdown: {request.IncludeDayWiseBreakdown}");

                // ✅ Get user
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException($"User with ID {userId} not found");

                // ✅ Build base query
                var baseQuery = _context.WashTransactions
                    .AsNoTracking()
                    .Include(t => t.WorkOrder)
                    .Include(t => t.ProcessStage)
                    .Include(t => t.CreatedByUser)
                    .Include(t => t.UpdatedByUser)
                    .Where(t => t.CreatedBy == userId && t.IsActive)
                    .AsQueryable();

                // ✅ Apply date filter if provided
                if (request.StartDate.HasValue)
                {
                    Console.WriteLine($"   Applying StartDate filter: {request.StartDate:yyyy-MM-dd}");
                    baseQuery = baseQuery.Where(t => t.TransactionDate.Date >= request.StartDate.Value.Date);
                }

                if (request.EndDate.HasValue)
                {
                    Console.WriteLine($"   Applying EndDate filter: {request.EndDate:yyyy-MM-dd}");
                    baseQuery = baseQuery.Where(t => t.TransactionDate.Date <= request.EndDate.Value.Date);
                }

                // ✅ Apply search
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    Console.WriteLine($"🔍 Applying search: {request.SearchTerm}");
                    baseQuery = baseQuery.SearchTransaction(request.SearchTerm);
                }

                // ✅ Apply other filters
                if (!string.IsNullOrEmpty(request.Buyer) ||
                    !string.IsNullOrEmpty(request.Factory) ||
                    !string.IsNullOrEmpty(request.Unit) ||
                    request.ProcessStageId.HasValue ||
                    request.TransactionTypeId.HasValue)
                {
                    Console.WriteLine("🎛️ Applying filters...");

                    if (!string.IsNullOrEmpty(request.Buyer))
                        baseQuery = baseQuery.Where(t =>
                            t.WorkOrder.Buyer.ToLower().Contains(request.Buyer.ToLower()));

                    if (!string.IsNullOrEmpty(request.Factory))
                        baseQuery = baseQuery.Where(t =>
                            t.WorkOrder.Factory.ToLower() == request.Factory.ToLower());

                    if (!string.IsNullOrEmpty(request.Unit))
                        baseQuery = baseQuery.Where(t =>
                            t.WorkOrder.Unit.ToLower() == request.Unit.ToLower());

                    if (request.ProcessStageId.HasValue)
                        baseQuery = baseQuery.Where(t =>
                            t.ProcessStageId == request.ProcessStageId.Value);

                    if (request.TransactionTypeId.HasValue)
                        baseQuery = baseQuery.Where(t =>
                            (int)t.TransactionType == request.TransactionTypeId.Value);
                }

                // ✅ Get all filtered transactions for summary
                var allTransactions = await baseQuery.ToListAsync();
                var totalCount = allTransactions.Count;

                Console.WriteLine($"📊 Total transactions: {totalCount}");

                // ✅ Calculate summary
                var totalReceiveCount = allTransactions.Count(t => t.TransactionType == TransactionType.Receive);
                var totalDeliveryCount = allTransactions.Count(t => t.TransactionType == TransactionType.Delivery);
                var totalReceivedQty = allTransactions
                    .Where(t => t.TransactionType == TransactionType.Receive)
                    .Sum(t => t.Quantity);
                var totalDeliveredQty = allTransactions
                    .Where(t => t.TransactionType == TransactionType.Delivery)
                    .Sum(t => t.Quantity);

                Console.WriteLine($"📈 Summary - Receives: {totalReceiveCount}, Deliveries: {totalDeliveryCount}");

                // ✅ Stage-wise summary
                var stageWiseSummary = new Dictionary<string, ProcessBalanceDto>();
                var stages = allTransactions.GroupBy(t => t.ProcessStageId).Select(g => g.Key).Distinct().ToList();

                foreach (var stageId in stages)
                {
                    var stageTransactions = allTransactions.Where(t => t.ProcessStageId == stageId).ToList();
                    if (stageTransactions.Count > 0)
                    {
                        var stage = await _context.ProcessStages.FindAsync(stageId);
                        var workOrderId = stageTransactions.First().WorkOrderId;
                        var workOrder = await _context.WorkOrders.FindAsync(workOrderId);

                        var balanceDto = new ProcessBalanceDto
                        {
                            WorkOrderId = workOrderId,
                            WorkOrderNo = workOrder?.WorkOrderNo ?? "-",
                            StyleName = workOrder?.StyleName ?? "-",
                            ProcessStageId = stageId,
                            ProcessStageName = stage?.Name ?? "-",
                            TotalReceived = stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Receive)
                                .Sum(t => t.Quantity),
                            TotalDelivered = stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Delivery)
                                .Sum(t => t.Quantity),
                            CurrentBalance = stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Receive)
                                .Sum(t => t.Quantity) -
                            stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Delivery)
                                .Sum(t => t.Quantity),
                            LastReceiveDate = stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Receive)
                                .OrderByDescending(t => t.TransactionDate)
                                .Select(t => (DateTime?)t.TransactionDate)
                                .FirstOrDefault(),
                            LastDeliveryDate = stageTransactions
                                .Where(t => t.TransactionType == TransactionType.Delivery)
                                .OrderByDescending(t => t.TransactionDate)
                                .Select(t => (DateTime?)t.TransactionDate)
                                .FirstOrDefault()
                        };
                        stageWiseSummary[stage?.Name ?? $"Stage {stageId}"] = balanceDto;
                    }
                }

                // ✅ Day-wise breakdown (only if requested and date range provided)
                List<DayWiseTransactionDto>? dayWiseBreakdown = null;
                if (request.IncludeDayWiseBreakdown && request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    Console.WriteLine("📅 Generating day-wise breakdown...");
                    dayWiseBreakdown = new List<DayWiseTransactionDto>();
                    var currentDate = request.StartDate.Value.Date;

                    while (currentDate <= request.EndDate.Value.Date)
                    {
                        var dayTransactions = allTransactions
                            .Where(t => t.TransactionDate.Date == currentDate)
                            .ToList();

                        if (dayTransactions.Count > 0)
                        {
                            dayWiseBreakdown.Add(new DayWiseTransactionDto
                            {
                                Date = currentDate,
                                DayReceiveCount = dayTransactions.Count(t => t.TransactionType == TransactionType.Receive),
                                DayDeliveryCount = dayTransactions.Count(t => t.TransactionType == TransactionType.Delivery),
                                DayReceivedQty = dayTransactions
                                    .Where(t => t.TransactionType == TransactionType.Receive)
                                    .Sum(t => t.Quantity),
                                DayDeliveredQty = dayTransactions
                                    .Where(t => t.TransactionType == TransactionType.Delivery)
                                    .Sum(t => t.Quantity),
                                Transactions = dayTransactions.Select(t => new WashTransactionResponseDto
                                {
                                    Id = t.Id,
                                    WorkOrderId = t.WorkOrderId,
                                    WorkOrderNo = t.WorkOrder.WorkOrderNo,
                                    StyleName = t.WorkOrder.StyleName,
                                    Buyer = t.WorkOrder.Buyer,
                                    Factory = t.WorkOrder.Factory,
                                    Line = t.WorkOrder.Line,
                                    TransactionType = t.TransactionType,
                                    TransactionTypeName = t.TransactionType.ToString(),
                                    ProcessStageId = t.ProcessStageId,
                                    ProcessStageName = t.ProcessStage.Name,
                                    Quantity = t.Quantity,
                                    TransactionDate = t.TransactionDate,
                                    BatchNo = t.BatchNo,
                                    GatePassNo = t.GatePassNo,
                                    Remarks = t.Remarks,
                                    ReceivedBy = t.ReceivedBy,
                                    DeliveredTo = t.DeliveredTo,
                                    CreatedBy = t.CreatedBy,
                                    CreatedByUsername = t.CreatedByUser.Username,
                                    CreatedAt = t.CreatedAt,
                                    UpdatedByUsername = t.UpdatedByUser?.Username,
                                    UpdatedAt = t.UpdatedAt
                                }).ToList()
                            });
                        }
                        currentDate = currentDate.AddDays(1);
                    }
                    Console.WriteLine($"   Day-wise breakdown: {dayWiseBreakdown.Count} days with data");
                }

                // ✅ Apply sorting and pagination
                var sortedQuery = baseQuery.ApplyTransactionSort(request.SortBy, request.SortOrder);
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);
                var skip = (request.Page - 1) * request.PageSize;

                var paginatedData = await sortedQuery
                    .Skip(skip)
                    .Take(request.PageSize)
                    .Select(t => new WashTransactionResponseDto
                    {
                        Id = t.Id,
                        WorkOrderId = t.WorkOrderId,
                        WorkOrderNo = t.WorkOrder.WorkOrderNo,
                        StyleName = t.WorkOrder.StyleName,
                        Buyer = t.WorkOrder.Buyer,
                        Factory = t.WorkOrder.Factory,
                        Line = t.WorkOrder.Line,
                        TransactionType = t.TransactionType,
                        TransactionTypeName = t.TransactionType.ToString(),
                        ProcessStageId = t.ProcessStageId,
                        ProcessStageName = t.ProcessStage.Name,
                        Quantity = t.Quantity,
                        TransactionDate = t.TransactionDate,
                        BatchNo = t.BatchNo,
                        GatePassNo = t.GatePassNo,
                        Remarks = t.Remarks,
                        ReceivedBy = t.ReceivedBy,
                        DeliveredTo = t.DeliveredTo,
                        CreatedBy = t.CreatedBy,
                        CreatedByUsername = t.CreatedByUser.Username,
                        CreatedAt = t.CreatedAt,
                        UpdatedByUsername = t.UpdatedByUser != null ? t.UpdatedByUser.Username : null,
                        UpdatedAt = t.UpdatedAt
                    })
                    .ToListAsync();

                Console.WriteLine($"✅ Page {request.Page}: {paginatedData.Count} transactions");

                // ✅ Build response
                return new UserTransactionSummaryDto
                {
                    UserId = userId,
                    Username = user.Username,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    TotalDays = request.StartDate.HasValue && request.EndDate.HasValue
                        ? (int)(request.EndDate.Value.Date - request.StartDate.Value.Date).TotalDays + 1
                        : null,
                    TotalTransactions = totalCount,
                    TotalReceiveCount = totalReceiveCount,
                    TotalDeliveryCount = totalDeliveryCount,
                    TotalReceivedQty = totalReceivedQty,
                    TotalDeliveredQty = totalDeliveredQty,
                    NetBalance = totalReceivedQty - totalDeliveredQty,
                    StageWiseSummary = stageWiseSummary,
                    DayWiseBreakdown = dayWiseBreakdown,
                    Transactions = new PaginatedResponseDto<WashTransactionResponseDto>
                    {
                        Success = true,
                        Message = totalCount == 0 ? "No transactions found" : null,
                        Data = paginatedData,
                        Pagination = new PaginationMetadata
                        {
                            CurrentPage = request.Page,
                            PageSize = request.PageSize,
                            TotalRecords = totalCount,
                            TotalPages = totalPages,
                            HasPrevious = request.Page > 1,
                            HasNext = request.Page < totalPages
                        }
                    }
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                throw;
            }
        }
        // ==========================================
        // EXPORT TO CSV WITH DATE FILTER
        // ==========================================
        /// <summary>
        /// Export transactions to CSV with all filters including date range
        /// Date Range Explanation:
        /// - startDate: Starting date (e.g., 2025-11-12)
        /// - endDate: Ending date (e.g., 2025-11-15)
        /// - Filters data from startDate to endDate (inclusive on both ends)
        /// </summary>
        // ==========================================
        // EXPORT TO CSV (SERVER-SIDE) - UPDATED
        // ==========================================
        public async Task<byte[]> ExportToCSVAsync(
            string? searchTerm = null,
            string? buyer = null,
            string? factory = null,
            string? unit = null, // ✅ ADDED
            int? processStageId = null,
            int? transactionTypeId = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                Console.WriteLine($"📥 CSV Export Request:");
                Console.WriteLine($"   SearchTerm: {searchTerm}");
                Console.WriteLine($"   Buyer: {buyer}");
                Console.WriteLine($"   Factory: {factory}");
                Console.WriteLine($"   Unit: {unit}"); // ✅ ADDED
                Console.WriteLine($"   ProcessStageId: {processStageId}");
                Console.WriteLine($"   TransactionTypeId: {transactionTypeId}");
                Console.WriteLine($"   StartDate: {startDate:yyyy-MM-dd}");
                Console.WriteLine($"   EndDate: {endDate:yyyy-MM-dd}");

                // ✅ Build query with all includes needed
                var query = _context.WashTransactions
                    .Include(t => t.WorkOrder)
                    .Include(t => t.ProcessStage)
                    .Where(t => t.IsActive)
                    .AsQueryable();

                // ✅ Apply Buyer filter
                if (!string.IsNullOrEmpty(buyer))
                {
                    Console.WriteLine($"   Applying Buyer filter: {buyer}");
                    query = query.Where(t => t.WorkOrder.Buyer.ToLower().Contains(buyer.ToLower()));
                }

                // ✅ Apply Factory filter
                if (!string.IsNullOrEmpty(factory))
                {
                    Console.WriteLine($"   Applying Factory filter: {factory}");
                    query = query.Where(t => t.WorkOrder.Factory.ToLower() == factory.ToLower());
                }

                // ✅ ADDED: Apply Unit filter
                if (!string.IsNullOrEmpty(unit))
                {
                    Console.WriteLine($"   Applying Unit filter: {unit}");
                    query = query.Where(t => t.WorkOrder.Unit.ToLower() == unit.ToLower());
                }

                // ✅ Apply ProcessStage filter
                if (processStageId.HasValue)
                {
                    Console.WriteLine($"   Applying ProcessStage filter: {processStageId}");
                    query = query.Where(t => t.ProcessStageId == processStageId.Value);
                }

                // ✅ Apply TransactionType filter
                if (transactionTypeId.HasValue)
                {
                    Console.WriteLine($"   Applying TransactionType filter: {transactionTypeId}");
                    query = query.Where(t => (int)t.TransactionType == transactionTypeId.Value);
                }

                // ✅ Apply Date Range Filter
                if (startDate.HasValue)
                {
                    Console.WriteLine($"   Applying StartDate filter: {startDate:yyyy-MM-dd}");
                    query = query.Where(t => t.TransactionDate.Date >= startDate.Value.Date);
                }

                if (endDate.HasValue)
                {
                    Console.WriteLine($"   Applying EndDate filter: {endDate:yyyy-MM-dd}");
                    query = query.Where(t => t.TransactionDate.Date <= endDate.Value.Date);
                }

                // ✅ Apply Search Term filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    Console.WriteLine($"   Applying SearchTerm filter: {searchTerm}");
                    var lowerSearchTerm = searchTerm.ToLower().Trim();
                    query = query.Where(t =>
                        t.WorkOrder.WorkOrderNo.ToLower().Contains(lowerSearchTerm) ||
                        t.WorkOrder.Buyer.ToLower().Contains(lowerSearchTerm) ||
                        t.WorkOrder.StyleName.ToLower().Contains(lowerSearchTerm) ||
                        t.WorkOrder.Factory.ToLower().Contains(lowerSearchTerm) ||
                        t.WorkOrder.Unit.ToLower().Contains(lowerSearchTerm) || // ✅ ADDED
                        t.ProcessStage.Name.ToLower().Contains(lowerSearchTerm) ||
                        (t.BatchNo != null && t.BatchNo.ToLower().Contains(lowerSearchTerm)) ||
                        (t.GatePassNo != null && t.GatePassNo.ToLower().Contains(lowerSearchTerm)) ||
                        t.Quantity.ToString().Contains(lowerSearchTerm)
                    );
                }

                // ✅ Execute query and get data
                var transactions = await query
                    .OrderByDescending(t => t.TransactionDate)
                    .ThenByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        Id = t.Id,
                        WorkOrderNo = t.WorkOrder.WorkOrderNo,
                        StyleName = t.WorkOrder.StyleName,
                        Buyer = t.WorkOrder.Buyer,
                        Factory = t.WorkOrder.Factory,
                        Unit = t.WorkOrder.Unit, // ✅ ADDED
                        FastReactNo = t.WorkOrder.FastReactNo ?? "-",
                        Color = t.WorkOrder.Color,
                        StageName = t.ProcessStage.Name,
                        TransactionType = t.TransactionType.ToString(),
                        Quantity = t.Quantity,
                        TransactionDate = t.TransactionDate,
                        BatchNo = t.BatchNo ?? "-",
                        GatePassNo = t.GatePassNo ?? "-",
                        Remarks = t.Remarks ?? "-",
                        ReceivedBy = t.ReceivedBy ?? "-",
                        DeliveredTo = t.DeliveredTo ?? "-",
                        CreatedAt = t.CreatedAt // ✅ CHANGED: Using CreatedAt
                    })
                    .ToListAsync();

                Console.WriteLine($"   Total records found: {transactions.Count}");

                if (transactions.Count == 0)
                {
                    Console.WriteLine($"❌ No transactions found for the given criteria");
                    throw new Exception("No transactions found matching your criteria");
                }

                Console.WriteLine($"📥 Generating CSV for {transactions.Count} transactions...");

                // ✅ Generate CSV content
                var stringBuilder = new StringBuilder();

                using (var stringWriter = new StringWriter(stringBuilder))
                {
                    using (var csv = new CsvWriter(stringWriter, CultureInfo.InvariantCulture))
                    {
                        // Write CSV Headers
                        csv.WriteField("ID");
                        csv.WriteField("Work Order No");
                        csv.WriteField("Style Name");
                        csv.WriteField("Buyer");
                        csv.WriteField("Factory");
                        csv.WriteField("Unit"); // ✅ ADDED
                        csv.WriteField("FastReact No");
                        csv.WriteField("Color");
                        csv.WriteField("Process Stage");
                        csv.WriteField("Transaction Type");
                        csv.WriteField("Quantity");
                        csv.WriteField("Transaction Date");
                        csv.WriteField("Batch No");
                        csv.WriteField("Gate Pass No");
                        csv.WriteField("Remarks");
                        csv.WriteField("Received By");
                        csv.WriteField("Delivered To");
                        csv.WriteField("Created At"); // ✅ CHANGED
                        csv.NextRecord(); // ✅ FIXED: No await

                        // Write CSV Data Rows
                        foreach (var transaction in transactions)
                        {
                            csv.WriteField(transaction.Id);
                            csv.WriteField(transaction.WorkOrderNo);
                            csv.WriteField(transaction.StyleName);
                            csv.WriteField(transaction.Buyer);
                            csv.WriteField(transaction.Factory);
                            csv.WriteField(transaction.Unit); // ✅ ADDED
                            csv.WriteField(transaction.FastReactNo);
                            csv.WriteField(transaction.Color);
                            csv.WriteField(transaction.StageName);
                            csv.WriteField(transaction.TransactionType);
                            csv.WriteField(transaction.Quantity);
                            csv.WriteField(transaction.TransactionDate.ToString("yyyy-MM-dd"));
                            csv.WriteField(transaction.BatchNo);
                            csv.WriteField(transaction.GatePassNo);
                            csv.WriteField(transaction.Remarks);
                            csv.WriteField(transaction.ReceivedBy);
                            csv.WriteField(transaction.DeliveredTo);
                            csv.WriteField(transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")); // ✅ CHANGED
                            csv.NextRecord(); // ✅ FIXED: No await
                        }

                        csv.Flush();
                    }
                }

                // ✅ Convert string to bytes
                var csvContent = stringBuilder.ToString();
                var result = Encoding.UTF8.GetBytes(csvContent);

                if (result.Length == 0)
                {
                    Console.WriteLine($"❌ CSV content is empty!");
                    throw new Exception("Failed to generate CSV content");
                }

                Console.WriteLine($"✅ CSV generated successfully!");
                Console.WriteLine($"   Size: {result.Length} bytes");
                Console.WriteLine($"   Records: {transactions.Count}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CSV Export Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                throw new Exception($"Error exporting transactions: {ex.Message}", ex);
            }
        }

        public async Task<ShiftSchedule> CreateScheduleAsync(CreateShiftScheduleDto dto, int userId)
        {
            var schedule = new ShiftSchedule
            {
                Name = dto.Name,
                DayShiftStart = dto.DayShiftStart,
                DayShiftEnd = dto.DayShiftEnd,
                NightShiftStart = dto.NightShiftStart,
                NightShiftEnd = dto.NightShiftEnd,
                EffectiveFromDate = dto.EffectiveFromDate,
                EffectiveToDate = dto.EffectiveToDate,
                IsActive = true,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShiftSchedules.Add(schedule);
            await _context.SaveChangesAsync();

            return schedule;
        }


        public async Task<ShiftSchedule?> GetActiveScheduleAsync(DateTime date)
        {
            return await _context.ShiftSchedules
                .Where(s => s.IsActive)
                .Where(s => s.EffectiveFromDate <= date)
                .Where(s => !s.EffectiveToDate.HasValue || s.EffectiveToDate >= date)
                .OrderByDescending(s => s.EffectiveFromDate)
                .FirstOrDefaultAsync();
        }

        
    }
}