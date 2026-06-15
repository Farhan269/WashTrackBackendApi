using System.ComponentModel.DataAnnotations;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Models
{
    public class WorkOrder
    {
        public int Id { get; set; }

        [StringLength(100)]
        public string Factory { get; set; } = string.Empty;

        [StringLength(100)]
        public string Line { get; set; } = string.Empty;

        [StringLength(100)]
        public string Unit { get; set; } = string.Empty;

        [StringLength(300)]
        public string Buyer { get; set; } = string.Empty;

        [StringLength(300)]
        public string BuyerDepartment { get; set; } = string.Empty;

        [StringLength(300)]
        public string StyleName { get; set; } = string.Empty;

        [StringLength(300)]
        public string FastReactNo { get; set; } = string.Empty;

        [StringLength(100)]
        public string Color { get; set; } = string.Empty;

        [StringLength(100)]
        public string WorkOrderNo { get; set; } = string.Empty;

        [StringLength(100)]
        public string WashType { get; set; } = string.Empty;

        // ✅ FIXED: Nullable ints to match Database
        public int? OrderQuantity { get; set; }
        public int? CutQty { get; set; }

        // Dates
        public DateTime? TOD { get; set; }
        public DateTime? SewingCompDate { get; set; }
        public DateTime? FirstRCVDate { get; set; }
        public DateTime? WashApprovalDate { get; set; }
        public DateTime? WashTargetDate { get; set; }

        // ✅ FIXED: Nullable ints to match Database
        public int? TotalWashReceived { get; set; }
        public int? TotalWashDelivery { get; set; }
        public int? WashBalance { get; set; }
        public int? FromReceived { get; set; }

        [StringLength(500)]
        public string? Marks { get; set; }

        // Audit Fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }

        // ✅ FIXED: Nullable bool/dates for sync info
        public bool? SyncedFromExternalApi { get; set; } = false;
        public DateTime? ExternalApiSyncDate { get; set; }
        [StringLength(500)]
        public string? ExternalApiSource { get; set; }

        // Navigation Properties
        public User CreatedByUser { get; set; } = null!;
        public User? UpdatedByUser { get; set; }

        [StringLength(100)]
        public string? FirstWashBatchQty { get; set; }
        [StringLength(100)]
        public string? FirstWashBatchTime{ get; set; }
        [StringLength(100)]
        public string? SecondWashBatchQty { get; set; }
        [StringLength(100)]
        public string? SecondWashBatchTime { get; set; }

        public long? Status { get; set; }
    }
}