
////using Microsoft.EntityFrameworkCore;
////using Newtonsoft.Json;
////using System.Globalization;
////using wsahRecieveDelivary.Data;
////using wsahRecieveDelivary.DTOs;
////using wsahRecieveDelivary.Models;

////namespace wsahRecieveDelivary.Services
////{
////    public class ExternalApiSyncService : IExternalApiSyncService
////    {
////        private readonly ApplicationDbContext _context;
////        private readonly HttpClient _httpClient;
////        private readonly ILogger<ExternalApiSyncService> _logger;
////        private readonly IConfiguration _configuration;

////        public ExternalApiSyncService(
////            ApplicationDbContext context,
////            HttpClient httpClient,
////            ILogger<ExternalApiSyncService> logger,
////            IConfiguration configuration)
////        {
////            _context = context;
////            _httpClient = httpClient;
////            _logger = logger;
////            _configuration = configuration;
////        }

////        public async Task<SyncResultDto> SyncWorkOrdersAsync()
////        {
////            var result = new SyncResultDto
////            {
////                SyncStartTime = DateTime.UtcNow,
////                Success = false
////            };

////            int upToDateCount = 0;

////            try
////            {
////                _logger.LogInformation("🔄 Starting work order sync from external API");

////                // 1. Fetch External Data
////                var externalWorkOrders = await FetchFromExternalApiAsync();
////                result.TotalRecordsFetched = externalWorkOrders.Count;

////                if (!externalWorkOrders.Any())
////                {
////                    result.Message = "No records found from external API";
////                    result.Success = true;
////                    result.SyncEndTime = DateTime.UtcNow;
////                    await SaveSyncLogAsync(result);
////                    return result;
////                }

////                // 2. Validate and Extract IDs (Filter invalid WOs)
////                var validExternalOrders = externalWorkOrders
////                    .Where(x => !string.IsNullOrWhiteSpace(x.WorkOrderNo))
////                    .ToList();

////                var externalWorkOrderNos = validExternalOrders
////                    .Select(x => x.WorkOrderNo.Trim())
////                    .Distinct()
////                    .ToList();

////                // 3. Bulk Fetch Existing Records from DB (Optimized)
////                var existingWorkOrdersDict = await _context.WorkOrders
////                    .Where(w => externalWorkOrderNos.Contains(w.WorkOrderNo))
////                    .ToDictionaryAsync(w => w.WorkOrderNo, w => w);

////                _logger.LogInformation("✅ Loaded {Count} existing records from DB", existingWorkOrdersDict.Count);

////                // 4. Process Records
////                foreach (var externalWo in validExternalOrders)
////                {
////                    try
////                    {
////                        var workOrderNo = externalWo.WorkOrderNo.Trim();

////                        // Parse Incoming Values
////                        var orderQty = ParseInt(externalWo.OrderQuantity.ToString());
////                        var totalWashRec = ParseInt(externalWo.TotalWashReceived.ToString());
////                        var totalWashDel = ParseInt(externalWo.TotalWashDelivery.ToString());
////                        var washBalance = ParseInt(externalWo.WashBalanceFromReceived.ToString());

////                        // Logic: Recalculate balance if 0 but activity exists
////                        if (washBalance == 0 && (totalWashRec > 0 || totalWashDel > 0))
////                        {
////                            washBalance = totalWashRec - totalWashDel;
////                        }

////                        if (existingWorkOrdersDict.TryGetValue(workOrderNo, out var existingWo))
////                        {
////                            // UPDATE LOGIC: Check if data changed
////                            bool isChanged = HasDataChanged(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

////                            if (isChanged)
////                            {
////                                MapExternalToEntity(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);
////                                existingWo.UpdatedBy = 1; // System User
////                                existingWo.UpdatedAt = DateTime.UtcNow;
////                                existingWo.SyncedFromExternalApi = true;
////                                existingWo.ExternalApiSyncDate = DateTime.UtcNow;
////                                result.UpdatedCount++;
////                            }
////                            else
////                            {
////                                upToDateCount++;
////                            }
////                        }
////                        else
////                        {
////                            // CREATE LOGIC
////                            var newWo = new WorkOrder
////                            {
////                                CreatedBy = 1, // System User
////                                CreatedAt = DateTime.UtcNow,
////                                SyncedFromExternalApi = true,
////                                ExternalApiSyncDate = DateTime.UtcNow,
////                                ExternalApiSource = _configuration["ExternalApi:WashPlansUrl"]
////                                    ?? "http://192.168.136.52:3000/api/washplans"
////                            };

////                            MapExternalToEntity(newWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

////                            // Initialize fields not in API
////                            newWo.CutQty = 0;
////                            newWo.SewingCompDate = null;
////                            newWo.FirstRCVDate = null;
////                            newWo.WashApprovalDate = null;

////                            _context.WorkOrders.Add(newWo);
////                            result.CreatedCount++;
////                        }
////                    }
////                    catch (Exception ex)
////                    {
////                        result.FailedCount++;
////                        result.Errors.Add($"WO: {externalWo.WorkOrderNo} - {ex.Message}");
////                    }
////                }

////                // 5. Save Changes
////                await _context.SaveChangesAsync();

////                result.Success = true;
////                result.SyncEndTime = DateTime.UtcNow;
////                result.Message = $"Sync Completed. Created: {result.CreatedCount}, Updated: {result.UpdatedCount}, UpToDate: {upToDateCount}, Failed: {result.FailedCount}";

////                _logger.LogInformation(result.Message);
////                await SaveSyncLogAsync(result);

////                return result;
////            }
////            catch (Exception ex)
////            {
////                result.Success = false;
////                result.Message = $"Critical Sync Error: {ex.Message}";
////                result.SyncEndTime = DateTime.UtcNow;
////                _logger.LogError(ex, "Sync failed globally");
////                await SaveSyncLogAsync(result);
////                return result;
////            }
////        }

////        // ✅ Helper: Check if data changed (Handles Nulls safely)
////        private bool HasDataChanged(WorkOrder db, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
////        {
////            if (!string.Equals(db.Factory, api.Factory?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.Line, api.SewingLine?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.Unit, api.Unit?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.Buyer, api.Buyer?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.StyleName, api.StyleName?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.FastReactNo, api.FastReactNo?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
////            if (!string.Equals(db.Color, api.Color?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

////            // Compare Integers (Use GetValueOrDefault to treat DB NULL as 0)
////            if (db.OrderQuantity.GetValueOrDefault() != orderQty) return true;
////            if (db.TotalWashReceived.GetValueOrDefault() != washRec) return true;
////            if (db.TotalWashDelivery.GetValueOrDefault() != washDel) return true;
////            if (db.WashBalance.GetValueOrDefault() != washBal) return true;
////            if (db.FromReceived.GetValueOrDefault() != washBal) return true;

////            // Compare Dates
////            if (db.TOD != api.Tod) return true;
////            if (db.WashTargetDate != api.WashTargetDate) return true;

////            if (!string.Equals(db.Marks, api.Marks?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

////            return false;
////        }

////        // ✅ Helper: Map data
////        private void MapExternalToEntity(WorkOrder wo, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
////        {
////            wo.WorkOrderNo = api.WorkOrderNo?.Trim();
////            wo.Factory = api.Factory?.Trim() ?? "";
////            wo.Line = api.SewingLine?.Trim() ?? "";
////            wo.Unit = api.Unit?.Trim() ?? "";
////            wo.Buyer = api.Buyer?.Trim() ?? "";
////            wo.BuyerDepartment = api.Buyer?.Trim() ?? "";
////            wo.StyleName = api.StyleName?.Trim() ?? "";
////            wo.FastReactNo = api.FastReactNo?.Trim() ?? "";
////            wo.WashType = api.FastReactNo?.Trim() ?? "";
////            wo.Color = api.Color?.Trim() ?? "";

////            wo.OrderQuantity = orderQty;
////            wo.TOD = api.Tod;
////            wo.WashTargetDate = api.WashTargetDate;
////            wo.TotalWashReceived = washRec;
////            wo.TotalWashDelivery = washDel;
////            wo.WashBalance = washBal;
////            wo.FromReceived = washBal;
////            wo.Marks = api.Marks?.Trim() ?? "";
////        }

////        private async Task<List<ExternalWorkOrderDto>> FetchFromExternalApiAsync()
////        {
////            try
////            {
////                var apiUrl = _configuration["ExternalApi:WashPlansUrl"]
////                    ?? "http://192.168.136.52:3000/api/washplans";

////                var response = await _httpClient.GetAsync(apiUrl);
////                response.EnsureSuccessStatusCode();

////                var jsonContent = await response.Content.ReadAsStringAsync();
////                var apiResponse = JsonConvert.DeserializeObject<ApiWashPlansResponse>(jsonContent);

////                return apiResponse?.WashPlans ?? new List<ExternalWorkOrderDto>();
////            }
////            catch (Exception ex)
////            {
////                _logger.LogError(ex, "Error fetching from external API");
////                throw;
////            }
////        }

////        private async Task SaveSyncLogAsync(SyncResultDto result)
////        {
////            var syncLog = new SyncLog
////            {
////                SyncType = "ExternalApiSync",
////                SourceApi = _configuration["ExternalApi:WashPlansUrl"] ?? "http://192.168.136.52:3000/api/washplans",
////                TotalRecordsFetched = result.TotalRecordsFetched,
////                UptoDate = result.upToDateCount,
////                CreatedCount = result.CreatedCount,
////                UpdatedCount = result.UpdatedCount,
////                FailedCount = result.FailedCount,
////                Success = result.Success,
////                ErrorMessage = string.Join("; ", result.Errors.Take(5)),
////                SyncStartTime = result.SyncStartTime,
////                SyncEndTime = result.SyncEndTime,
////                Duration = (result.SyncEndTime ?? DateTime.UtcNow) - result.SyncStartTime,
////                CreatedAt = DateTime.UtcNow
////            };

////            _context.SyncLogs.Add(syncLog);
////            await _context.SaveChangesAsync();
////        }

////        public async Task<SyncStatusDto> GetLastSyncStatusAsync()
////        {
////            var lastSync = await _context.SyncLogs
////                .Where(s => s.SyncType == "ExternalApiSync")
////                .OrderByDescending(s => s.CreatedAt)
////                .FirstOrDefaultAsync();

////            if (lastSync == null)
////            {
////                return new SyncStatusDto { HasSynced = false, Message = "No sync history found" };
////            }

////            return new SyncStatusDto
////            {
////                HasSynced = true,
////                LastSyncTime = lastSync.SyncStartTime,
////                Duration = lastSync.Duration,
////                TotalRecords = lastSync.TotalRecordsFetched,
////                //UptoDate = lastSync.upToDateCount,
////                CreatedCount = lastSync.CreatedCount,
////                UpdatedCount = lastSync.UpdatedCount,
////                FailedCount = lastSync.FailedCount,
////                Success = lastSync.Success,
////                Message = lastSync.ErrorMessage ?? "Sync completed successfully"
////            };
////        }

////        public async Task<SyncResultDto> AutoSyncWorkOrdersAsync()
////        {
////            return await SyncWorkOrdersAsync();
////        }

////        private int ParseInt(string? value)
////        {
////            if (string.IsNullOrWhiteSpace(value)) return 0;
////            value = value.Replace(",", "").Replace(" ", "").Trim();
////            return int.TryParse(value, out int result) ? result : 0;
////        }
////    }
////}


//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using System.Globalization;
//using wsahRecieveDelivary.Data;
//using wsahRecieveDelivary.DTOs;
//using wsahRecieveDelivary.Models;

//namespace wsahRecieveDelivary.Services
//{
//    public class ExternalApiSyncService : IExternalApiSyncService
//    {
//        private readonly ApplicationDbContext _context;
//        private readonly HttpClient _httpClient;
//        private readonly ILogger<ExternalApiSyncService> _logger;
//        private readonly IConfiguration _configuration;

//        public ExternalApiSyncService(
//            ApplicationDbContext context,
//            HttpClient httpClient,
//            ILogger<ExternalApiSyncService> logger,
//            IConfiguration configuration)
//        {
//            _context = context;
//            _httpClient = httpClient;
//            _logger = logger;
//            _configuration = configuration;
//        }

//        public async Task<SyncResultDto> SyncWorkOrdersAsync()
//        {
//            var result = new SyncResultDto
//            {
//                SyncStartTime = DateTime.UtcNow,
//                Success = false,
//                UpToDateCount = 0 // Initialize
//            };

//            try
//            {
//                _logger.LogInformation("🔄 Starting work order sync from external API");

//                var externalWorkOrders = await FetchFromExternalApiAsync();
//                result.TotalRecordsFetched = externalWorkOrders.Count;

//                if (!externalWorkOrders.Any())
//                {
//                    result.Message = "No records found from external API";
//                    result.Success = true;
//                    result.SyncEndTime = DateTime.UtcNow;
//                    await SaveSyncLogAsync(result);
//                    return result;
//                }

//                var validExternalOrders = externalWorkOrders
//                    .Where(x => !string.IsNullOrWhiteSpace(x.WorkOrderNo))
//                    .ToList();

//                var externalWorkOrderNos = validExternalOrders
//                    .Select(x => x.WorkOrderNo.Trim())
//                    .Distinct()
//                    .ToList();

//                var existingWorkOrdersDict = await _context.WorkOrders
//                    .Where(w => externalWorkOrderNos.Contains(w.WorkOrderNo))
//                    .ToDictionaryAsync(w => w.WorkOrderNo, w => w);

//                _logger.LogInformation("✅ Loaded {Count} existing records from DB", existingWorkOrdersDict.Count);

//                foreach (var externalWo in validExternalOrders)
//                {
//                    try
//                    {
//                        var workOrderNo = externalWo.WorkOrderNo.Trim();

//                        var orderQty = ParseInt(externalWo.OrderQuantity.ToString());
//                        var totalWashRec = ParseInt(externalWo.TotalWashReceived.ToString());
//                        var totalWashDel = ParseInt(externalWo.TotalWashDelivery.ToString());
//                        var washBalance = ParseInt(externalWo.WashBalanceFromReceived.ToString());

//                        if (washBalance == 0 && (totalWashRec > 0 || totalWashDel > 0))
//                        {
//                            washBalance = totalWashRec - totalWashDel;
//                        }

//                        if (existingWorkOrdersDict.TryGetValue(workOrderNo, out var existingWo))
//                        {
//                            bool isChanged = HasDataChanged(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

//                            if (isChanged)
//                            {
//                                MapExternalToEntity(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);
//                                existingWo.UpdatedBy = 1;
//                                existingWo.UpdatedAt = DateTime.UtcNow;
//                                existingWo.SyncedFromExternalApi = true;
//                                existingWo.ExternalApiSyncDate = DateTime.UtcNow;
//                                result.UpdatedCount++;
//                            }
//                            else
//                            {
//                                // ✅ CHANGED: Update DTO property directly
//                                result.UpToDateCount++;
//                            }
//                        }
//                        else
//                        {
//                            var newWo = new WorkOrder
//                            {
//                                CreatedBy = 1,
//                                CreatedAt = DateTime.UtcNow,
//                                SyncedFromExternalApi = true,
//                                ExternalApiSyncDate = DateTime.UtcNow,
//                                ExternalApiSource = _configuration["ExternalApi:WashPlansUrl"]
//                                    ?? "http://192.168.136.52:3000/api/washplans"
//                            };

//                            MapExternalToEntity(newWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

//                            newWo.CutQty = 0;
//                            newWo.SewingCompDate = null;
//                            newWo.FirstRCVDate = null;
//                            newWo.WashApprovalDate = null;

//                            _context.WorkOrders.Add(newWo);
//                            result.CreatedCount++;
//                        }
//                    }
//                    catch (Exception ex)
//                    {
//                        result.FailedCount++;
//                        result.Errors.Add($"WO: {externalWo.WorkOrderNo} - {ex.Message}");
//                    }
//                }

//                await _context.SaveChangesAsync();

//                result.Success = true;
//                result.SyncEndTime = DateTime.UtcNow;
//                // ✅ CHANGED: Use result.UpToDateCount
//                result.Message = $"Sync Completed. Created: {result.CreatedCount}, Updated: {result.UpdatedCount}, UpToDate: {result.UpToDateCount}, Failed: {result.FailedCount}";

//                _logger.LogInformation(result.Message);
//                await SaveSyncLogAsync(result);

//                return result;
//            }
//            catch (Exception ex)
//            {
//                result.Success = false;
//                result.Message = $"Critical Sync Error: {ex.Message}";
//                result.SyncEndTime = DateTime.UtcNow;
//                _logger.LogError(ex, "Sync failed globally");
//                await SaveSyncLogAsync(result);
//                return result;
//            }
//        }

//        private bool HasDataChanged(WorkOrder db, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
//        {
//            if (!string.Equals(db.Factory, api.Factory?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.Line, api.SewingLine?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.Unit, api.Unit?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.Buyer, api.Buyer?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.StyleName, api.StyleName?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.FastReactNo, api.FastReactNo?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
//            if (!string.Equals(db.Color, api.Color?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

//            if (db.OrderQuantity.GetValueOrDefault() != orderQty) return true;
//            if (db.TotalWashReceived.GetValueOrDefault() != washRec) return true;
//            if (db.TotalWashDelivery.GetValueOrDefault() != washDel) return true;
//            if (db.WashBalance.GetValueOrDefault() != washBal) return true;
//            if (db.FromReceived.GetValueOrDefault() != washBal) return true;

//            if (db.TOD != api.Tod) return true;
//            if (db.WashTargetDate != api.WashTargetDate) return true;

//            if (!string.Equals(db.Marks, api.Marks?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;

//            return false;
//        }

//        private void MapExternalToEntity(WorkOrder wo, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
//        {
//            wo.WorkOrderNo = api.WorkOrderNo?.Trim();
//            wo.Factory = api.Factory?.Trim() ?? "";
//            wo.Line = api.SewingLine?.Trim() ?? "";
//            wo.Unit = api.Unit?.Trim() ?? "";
//            wo.Buyer = api.Buyer?.Trim() ?? "";
//            wo.BuyerDepartment = api.Buyer?.Trim() ?? "";
//            wo.StyleName = api.StyleName?.Trim() ?? "";
//            wo.FastReactNo = api.FastReactNo?.Trim() ?? "";
//            wo.WashType = api.FastReactNo?.Trim() ?? "";
//            wo.Color = api.Color?.Trim() ?? "";

//            wo.OrderQuantity = orderQty;
//            wo.TOD = api.Tod;
//            wo.WashTargetDate = api.WashTargetDate;
//            wo.TotalWashReceived = washRec;
//            wo.TotalWashDelivery = washDel;
//            wo.WashBalance = washBal;
//            wo.FromReceived = washBal;
//            wo.Marks = api.Marks?.Trim() ?? "";
//        }

//        private async Task<List<ExternalWorkOrderDto>> FetchFromExternalApiAsync()
//        {
//            var apiUrl = _configuration["ExternalApi:WashPlansUrl"] ?? "http://192.168.136.52:3000/api/washplans";
//            var response = await _httpClient.GetAsync(apiUrl);
//            response.EnsureSuccessStatusCode();
//            var jsonContent = await response.Content.ReadAsStringAsync();
//            var apiResponse = JsonConvert.DeserializeObject<ApiWashPlansResponse>(jsonContent);
//            return apiResponse?.WashPlans ?? new List<ExternalWorkOrderDto>();
//        }

//        private async Task SaveSyncLogAsync(SyncResultDto result)
//        {
//            var syncLog = new SyncLog
//            {
//                SyncType = "ExternalApiSync",
//                SourceApi = _configuration["ExternalApi:WashPlansUrl"] ?? "http://192.168.136.52:3000/api/washplans",
//                TotalRecordsFetched = result.TotalRecordsFetched,
//                CreatedCount = result.CreatedCount,
//                UpdatedCount = result.UpdatedCount,

//                // ✅ CRITICAL: This line saves the data to the database
//                UpToDateCount = result.UpToDateCount,

//                FailedCount = result.FailedCount,
//                Success = result.Success,
//                ErrorMessage = string.Join("; ", result.Errors.Take(5)),
//                SyncStartTime = result.SyncStartTime,
//                SyncEndTime = result.SyncEndTime,
//                Duration = (result.SyncEndTime ?? DateTime.UtcNow) - result.SyncStartTime,
//                CreatedAt = DateTime.UtcNow
//            };

//            _context.SyncLogs.Add(syncLog);
//            await _context.SaveChangesAsync();
//        }

//        public async Task<SyncStatusDto> GetLastSyncStatusAsync()
//        {
//            var lastSync = await _context.SyncLogs
//                .Where(s => s.SyncType == "ExternalApiSync")
//                .OrderByDescending(s => s.CreatedAt)
//                .FirstOrDefaultAsync();

//            if (lastSync == null)
//            {
//                return new SyncStatusDto
//                {
//                    HasSynced = false,
//                    Message = "No sync history found"
//                };
//            }

//            return new SyncStatusDto
//            {
//                HasSynced = true,
//                LastSyncTime = lastSync.SyncStartTime,
//                Duration = lastSync.Duration,
//                TotalRecords = lastSync.TotalRecordsFetched,
//                CreatedCount = lastSync.CreatedCount,
//                UpdatedCount = lastSync.UpdatedCount,
//                FailedCount = lastSync.FailedCount,
//                Success = lastSync.Success,
//                // Optional: You can append UpToDate info to the message if you want to see it in status checks
//                Message = $"{lastSync.ErrorMessage} (UpToDate: {lastSync.UpToDateCount})"
//            };
//        }

//        public async Task<SyncResultDto> AutoSyncWorkOrdersAsync() => await SyncWorkOrdersAsync();

//        private int ParseInt(string? value)
//        {
//            if (string.IsNullOrWhiteSpace(value)) return 0;
//            value = value.Replace(",", "").Replace(" ", "").Trim();
//            return int.TryParse(value, out int result) ? result : 0;
//        }
//    }
//}



using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;
using wsahRecieveDelivary.Helpers; // ✅ Add this namespace

namespace wsahRecieveDelivary.Services
{
    public class ExternalApiSyncService : IExternalApiSyncService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExternalApiSyncService> _logger;
        private readonly IConfiguration _configuration;

        public ExternalApiSyncService(
            ApplicationDbContext context,
            HttpClient httpClient,
            ILogger<ExternalApiSyncService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        private static readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);

        public async Task<SyncResultDto> SyncWorkOrdersAsync()
        {
            // ✅ PROFESSIONAL: Get BD Time via TimeZoneInfo
            var bdTime = DateTimeHelper.GetBangladeshTime();

            var result = new SyncResultDto
            {
                SyncStartTime = bdTime,
                Success = false,
                UpToDateCount = 0
            };

            // ✅ CRITICAL: Prevent concurrent syncs (Auto Sync + Manual Sync at same time)
            if (!await _syncLock.WaitAsync(0))
            {
                _logger.LogWarning("⚠️ Sync skipped because another sync is already in progress.");
                result.Message = "Sync skipped: Another sync process is running.";
                result.Success = false;
                return result;
            }

            try
            {
                _logger.LogInformation("🔄 Starting work order sync");

                var externalWorkOrders = await FetchFromExternalApiAsync();
                result.TotalRecordsFetched = externalWorkOrders.Count;

                if (!externalWorkOrders.Any())
                {
                    result.Message = "No records found from external API";
                    result.Success = true;
                    // ✅ Use Helper
                    result.SyncEndTime = DateTimeHelper.GetBangladeshTime();
                    await SaveSyncLogAsync(result);
                    return result;
                }

                // ✅ PROFESSIONAL APPROACH: Group by WorkOrderNo and MERGE data instead of discarding
                var validExternalOrders = externalWorkOrders
                    .Where(x => !string.IsNullOrWhiteSpace(x.WorkOrderNo))
                    .GroupBy(x => x.WorkOrderNo.Trim())
                    .Select(g => g.First()) // Just take the first one and ignore duplicates
                    .ToList();

                var externalWorkOrderNos = validExternalOrders
                    .Select(x => x.WorkOrderNo.Trim())
                    .Distinct()
                    .ToList();

                // ✅ FIX: Use case-insensitive dictionary lookup to prevent duplicate inserts
                var existingWorkOrdersList = await _context.WorkOrders
                    .Where(w => externalWorkOrderNos.Contains(w.WorkOrderNo))
                    .ToListAsync();

                var existingWorkOrdersDict = existingWorkOrdersList
                    .GroupBy(w => w.WorkOrderNo.Trim(), StringComparer.OrdinalIgnoreCase) // Group by Case-Insensitive Key
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                foreach (var externalWo in validExternalOrders)
                {
                    try
                    {
                        var workOrderNo = externalWo.WorkOrderNo.Trim();

                        var orderQty = ParseInt(externalWo.OrderQuantity.ToString());
                        var totalWashRec = ParseInt(externalWo.TotalWashReceived.ToString());
                        var totalWashDel = ParseInt(externalWo.TotalWashDelivery.ToString());
                        var washBalance = ParseInt(externalWo.WashBalanceFromReceived.ToString());

                        if (washBalance == 0 && (totalWashRec > 0 || totalWashDel > 0))
                        {
                            washBalance = totalWashRec - totalWashDel;
                        }

                        if (existingWorkOrdersDict.TryGetValue(workOrderNo, out var existingWo))
                        {
                            bool isChanged = HasDataChanged(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

                            if (isChanged)
                            {
                                MapExternalToEntity(existingWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);
                                existingWo.UpdatedBy = 1;
                                // ✅ Use Helper
                                existingWo.UpdatedAt = DateTimeHelper.GetBangladeshTime();
                                existingWo.SyncedFromExternalApi = true;
                                existingWo.ExternalApiSyncDate = DateTimeHelper.GetBangladeshTime();
                                result.UpdatedCount++;
                            }
                            else
                            {
                                result.UpToDateCount++;
                            }
                        }
                        else
                        {
                            var newWo = new WorkOrder
                            {
                                CreatedBy = 1,
                                // ✅ Use Helper
                                CreatedAt = DateTimeHelper.GetBangladeshTime(),
                                SyncedFromExternalApi = true,
                                ExternalApiSyncDate = DateTimeHelper.GetBangladeshTime(),
                                ExternalApiSource = _configuration["ExternalApi:WashPlansUrl"]
                                    ?? "http://192.168.136.52:3000/api/washplans"
                            };

                            MapExternalToEntity(newWo, externalWo, orderQty, totalWashRec, totalWashDel, washBalance);

                            newWo.CutQty = 0;
                            newWo.SewingCompDate = null;
                            newWo.FirstRCVDate = null;
                            newWo.WashApprovalDate = null;

                            _context.WorkOrders.Add(newWo);
                            result.CreatedCount++;
                            
                            // ✅ Add to local dict to prevent dupes in same batch if logic fails (safety net)
                            if (!existingWorkOrdersDict.ContainsKey(workOrderNo))
                            {
                                existingWorkOrdersDict.Add(workOrderNo, newWo);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"WO: {externalWo.WorkOrderNo} - {ex.Message}");
                    }
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                // ✅ Use Helper
                result.SyncEndTime = DateTimeHelper.GetBangladeshTime();
                result.Message = $"Sync Completed. Created: {result.CreatedCount}, Updated: {result.UpdatedCount}, UpToDate: {result.UpToDateCount}, Failed: {result.FailedCount}";

                _logger.LogInformation(result.Message);
                await SaveSyncLogAsync(result);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Critical Sync Error: {ex.Message}";
                // ✅ Use Helper
                result.SyncEndTime = DateTimeHelper.GetBangladeshTime();
                _logger.LogError(ex, "Sync failed globally");
                await SaveSyncLogAsync(result);
                return result;
            }
            finally
            {
                _syncLock.Release();
            }
        }

        // Keep HasDataChanged and MapExternalToEntity methods as they were...

        // Keep FetchFromExternalApiAsync method as it was...

        private async Task SaveSyncLogAsync(SyncResultDto result)
        {
            // ✅ Use Helper for "Now"
            var bdNow = DateTimeHelper.GetBangladeshTime();

            var syncLog = new SyncLog
            {
                SyncType = "ExternalApiSync",
                SourceApi = _configuration["ExternalApi:WashPlansUrl"] ?? "http://192.168.136.52:3000/api/washplans",
                TotalRecordsFetched = result.TotalRecordsFetched,
                CreatedCount = result.CreatedCount,
                UpdatedCount = result.UpdatedCount,
                UpToDateCount = result.UpToDateCount,
                FailedCount = result.FailedCount,
                Success = result.Success,
                ErrorMessage = string.Join("; ", result.Errors.Take(5)),
                SyncStartTime = result.SyncStartTime,
                SyncEndTime = result.SyncEndTime,
                Duration = (result.SyncEndTime ?? bdNow) - result.SyncStartTime,
                CreatedAt = bdNow
            };

            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();
        }

        // Keep GetLastSyncStatusAsync and other helpers as they were...

        // (Include the rest of the file content like MapExternalToEntity, HasDataChanged, etc here...)
        private bool HasDataChanged(WorkOrder db, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
        {
            // (Same as before)
            if (!string.Equals(db.Factory, api.Factory?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.Line, api.SewingLine?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.Unit, api.Unit?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.Buyer, api.Buyer?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.StyleName, api.StyleName?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.FastReactNo, api.FastReactNo?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.Color, api.Color?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (db.OrderQuantity.GetValueOrDefault() != orderQty) return true;
            if (db.TotalWashReceived.GetValueOrDefault() != washRec) return true;
            if (db.TotalWashDelivery.GetValueOrDefault() != washDel) return true;
            if (db.WashBalance.GetValueOrDefault() != washBal) return true;
            if (db.FromReceived.GetValueOrDefault() != washBal) return true;
            if (db.TOD != api.Tod) return true;
            if (db.WashTargetDate != api.WashTargetDate) return true;
            if (!string.Equals(db.Marks, api.Marks?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.FirstWashBatchQty, api.FirstWashBatchQty?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.FirstWashBatchTime, api.FirstWashBatchTime?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.SecondWashBatchQty, api.SecondWashBatchQty?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            if (!string.Equals(db.SecondWashBatchTime, api.SecondWashBatchTime?.Trim(), StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private void MapExternalToEntity(WorkOrder wo, ExternalWorkOrderDto api, int orderQty, int washRec, int washDel, int washBal)
        {
            // (Same as before)
            wo.WorkOrderNo = api.WorkOrderNo?.Trim();
            wo.Factory = api.Factory?.Trim() ?? "";
            wo.Line = api.SewingLine?.Trim() ?? "";
            wo.Unit = api.Unit?.Trim() ?? "";
            wo.Buyer = api.Buyer?.Trim() ?? "";
            wo.BuyerDepartment = api.Buyer?.Trim() ?? "";
            wo.StyleName = api.StyleName?.Trim() ?? "";
            wo.FastReactNo = api.FastReactNo?.Trim() ?? "";
            wo.WashType = api.FastReactNo?.Trim() ?? "";
            wo.Color = api.Color?.Trim() ?? "";
            wo.OrderQuantity = orderQty;
            wo.TOD = api.Tod;
            wo.WashTargetDate = api.WashTargetDate;
            wo.TotalWashReceived = washRec;
            wo.TotalWashDelivery = washDel;
            wo.WashBalance = washBal;
            wo.FromReceived = washBal;
            wo.Marks = api.Marks?.Trim() ?? "";
            wo.FirstWashBatchQty = api.FirstWashBatchQty ?? "";
            wo.FirstWashBatchTime = api.FirstWashBatchTime ?? "";
            wo.SecondWashBatchQty = api.SecondWashBatchQty ?? "";
            wo.SecondWashBatchTime = api.SecondWashBatchTime ?? "";
        }

        private async Task<List<ExternalWorkOrderDto>> FetchFromExternalApiAsync()
        {
            var apiUrl = _configuration["ExternalApi:WashPlansUrl"] ?? "http://192.168.136.52:3000/api/washplans";
            // ✅ Read timeout from config or default to 300 seconds (5 minutes)
            var timeoutSeconds = _configuration.GetValue<int>("ExternalApi:TimeoutSeconds", 300);
            
            _httpClient.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();
            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonConvert.DeserializeObject<ApiWashPlansResponse>(jsonContent);
            return apiResponse?.WashPlans ?? new List<ExternalWorkOrderDto>();
        }

        public async Task<SyncStatusDto> GetLastSyncStatusAsync()
        {
            var lastSync = await _context.SyncLogs
                .Where(s => s.SyncType == "ExternalApiSync")
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastSync == null) return new SyncStatusDto { HasSynced = false, Message = "No sync history found" };

            return new SyncStatusDto
            {
                HasSynced = true,
                LastSyncTime = lastSync.SyncStartTime,
                Duration = lastSync.Duration,
                TotalRecords = lastSync.TotalRecordsFetched,
                CreatedCount = lastSync.CreatedCount,
                UpdatedCount = lastSync.UpdatedCount,
                FailedCount = lastSync.FailedCount,
                Success = lastSync.Success,
                Message = lastSync.ErrorMessage
            };
        }

        public async Task<SyncResultDto> AutoSyncWorkOrdersAsync() => await SyncWorkOrdersAsync();

        private int ParseInt(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return 0;
            value = value.Replace(",", "").Replace(" ", "").Trim();
            return int.TryParse(value, out int result) ? result : 0;
        }
    }
}