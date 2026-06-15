using Newtonsoft.Json;

namespace wsahRecieveDelivary.DTOs
{
    // ==========================================
    // EXTERNAL API RESPONSE DTOs
    // ==========================================
    public class ApiWashPlansResponse
    {
        [JsonProperty("washPlans")]
        public List<ExternalWorkOrderDto> WashPlans { get; set; } = new();
    }

    public class ExternalWorkOrderDto
    {
        [JsonProperty("factory")]
        public string Factory { get; set; }

        [JsonProperty("sewingLine")]
        public string SewingLine { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("buyer")]
        public string Buyer { get; set; }

        [JsonProperty("workOrderNo")]
        public string WorkOrderNo { get; set; }

        [JsonProperty("styleName")]
        public string StyleName { get; set; }

        [JsonProperty("fastReactNo")]
        public string FastReactNo { get; set; }

        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("orderQuantity")]
        public int OrderQuantity { get; set; }

        [JsonProperty("tod")]
        public DateTime? Tod { get; set; }

        [JsonProperty("washTargetDate")]
        public DateTime? WashTargetDate { get; set; }

        [JsonProperty("totalWashReceived")]
        public int TotalWashReceived { get; set; }

        [JsonProperty("totalWashDelivery")]
        public int TotalWashDelivery { get; set; }

        [JsonProperty("washBalanceFromReceived")]
        public int WashBalanceFromReceived { get; set; }

        [JsonProperty("rewashBalance")]
        public int RewashBalance { get; set; }

        [JsonProperty("marks")]
        public string? Marks { get; set; }

        [JsonProperty("firstWashBatchQty")]
        public string? FirstWashBatchQty { get; set; }

        [JsonProperty("firstWashBatchTime")]
        public string? FirstWashBatchTime { get; set; }

        [JsonProperty("secondWashBatchQty")]
        public string? SecondWashBatchQty { get; set; }

        [JsonProperty("secondWashBatchTime")]
        public string? SecondWashBatchTime{ get; set; }
        [JsonProperty("Status")]
        public long? Status { get; set; }

    }

    // ==========================================
    // SYNC RESULT DTOs
    // ==========================================
    public class SyncResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int TotalRecordsFetched { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public int UpToDateCount { get; set; }
        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class SyncStatusDto
    {
        public bool HasSynced { get; set; }
        public DateTime? LastSyncTime { get; set; }
        public TimeSpan? Duration { get; set; }
        public int TotalRecords { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}