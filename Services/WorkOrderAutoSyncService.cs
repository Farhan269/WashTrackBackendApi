//using wsahRecieveDelivary.Services;

//namespace wsahRecieveDelivary.Services
//{
//    public class WorkOrderAutoSyncService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<WorkOrderAutoSyncService> _logger;
//        private readonly int _syncIntervalMinutes = 60;  

//        public WorkOrderAutoSyncService(
//            IServiceProvider serviceProvider,
//            ILogger<WorkOrderAutoSyncService> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            _logger.LogInformation("✅ Work Order Auto Sync Service started (every {Minutes} minutes)", _syncIntervalMinutes);

//            // ✅ Wait 1 minute before first sync (let app startup complete)
//            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    _logger.LogInformation("🔄 Auto-sync triggered at {Time}", DateTime.UtcNow);

//                    // ✅ Create scope for each sync (dependency injection)
//                    using (var scope = _serviceProvider.CreateScope())
//                    {
//                        var syncService = scope.ServiceProvider
//                            .GetRequiredService<IExternalApiSyncService>();

//                        var result = await syncService.SyncWorkOrdersAsync();

//                        _logger.LogInformation(
//                            "✅ Auto-sync completed | Created: {Created}, UptoDate:{UpToDateCount} Updated: {Updated}, Failed: {Failed}",
//                            result.CreatedCount, result.UpdatedCount, result.FailedCount);

//                        if (result.FailedCount > 0)
//                        {
//                            _logger.LogWarning("⚠️ Sync had {FailedCount} failures", result.FailedCount);
//                            foreach (var error in result.Errors.Take(5))
//                            {
//                                _logger.LogWarning("   - {Error}", error);
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "❌ Auto-sync failed");
//                }

//                // ✅ Wait 15 minutes before next sync
//                try
//                {
//                    await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
//                }
//                catch (OperationCanceledException)
//                {
//                    _logger.LogInformation("Auto-sync service stopping...");
//                    break;
//                }
//            }

//            _logger.LogInformation("🛑 Work Order Auto Sync Service stopped");
//        }
//    }
//}


using wsahRecieveDelivary.Helpers; // For DateTimeHelper

namespace wsahRecieveDelivary.Services
{
    public class WorkOrderAutoSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<WorkOrderAutoSyncService> _logger;
        private readonly int _syncIntervalMinutes;

        public WorkOrderAutoSyncService(
            IServiceProvider serviceProvider,
            ILogger<WorkOrderAutoSyncService> logger,
            IConfiguration configuration) // ✅ Inject Configuration
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            // ✅ Read from config or default to 60
            _syncIntervalMinutes = configuration.GetValue<int>("SyncSettings:IntervalMinutes", 60);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ Work Order Auto Sync Service started (every {Minutes} minutes)", _syncIntervalMinutes);

            // Wait 1 minute before first sync to let app startup/migrations complete
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // ✅ Use your Helper for consistent Bangladesh Time logging
                    _logger.LogInformation("🔄 Auto-sync triggered at {Time}", DateTimeHelper.GetBangladeshTime());

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var syncService = scope.ServiceProvider
                            .GetRequiredService<IExternalApiSyncService>();

                        var result = await syncService.SyncWorkOrdersAsync();

                        // ✅ FIXED: Added result.UpToDateCount to the arguments
                        _logger.LogInformation(
                            "✅ Auto-sync completed | Created: {Created}, UpToDate: {UpToDate}, Updated: {Updated}, Failed: {Failed}",
                            result.CreatedCount, result.UpToDateCount, result.UpdatedCount, result.FailedCount);

                        if (result.FailedCount > 0)
                        {
                            _logger.LogWarning("⚠️ Sync had {FailedCount} failures", result.FailedCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Auto-sync failed");
                }

                // Wait for next interval
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_syncIntervalMinutes), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Normal shutdown
                    break;
                }
            }

            _logger.LogInformation("🛑 Work Order Auto Sync Service stopped");
        }
    }
}