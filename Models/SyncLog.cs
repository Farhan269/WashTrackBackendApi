using System.ComponentModel.DataAnnotations;

namespace wsahRecieveDelivary.Models
{
    public class SyncLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SyncType { get; set; } = "ExternalApiSync";

        [Required]
        [StringLength(500)]
        public string SourceApi { get; set; } = string.Empty;

        public int TotalRecordsFetched { get; set; }
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public int FailedCount { get; set; }
        public int UpToDateCount { get; set; }
        public bool Success { get; set; }

        [StringLength(2000)]
        public string? ErrorMessage { get; set; }

        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public TimeSpan Duration { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}