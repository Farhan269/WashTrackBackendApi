using System.ComponentModel.DataAnnotations;
using wsahRecieveDelivary.Models.Enums;

namespace wsahRecieveDelivary.Models
{
    public class WashTransaction
    {
        public int Id { get; set; }

        [Required]
        public int WorkOrderId { get; set; }

        [Required]
        public TransactionType TransactionType { get; set; }

        [Required]
        public int ProcessStageId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public decimal Quantity { get; set; }

        [Required]
        // ✅ CHANGED: This stores the date when transaction happened (usually today)
        public DateTime TransactionDate { get; set; }

        [StringLength(100)]
        public string? BatchNo { get; set; }

        [StringLength(100)]
        public string? GatePassNo { get; set; }

        [StringLength(500)]
        public string? Remarks { get; set; }

        [StringLength(100)]
        public string? ReceivedBy { get; set; }

        [StringLength(100)]
        public string? DeliveredTo { get; set; }

        // ==========================================
        // AUDIT FIELDS
        // ==========================================
        public int CreatedBy { get; set; }

        [Required]
        // ✅ CHANGED: When record was created in system (automatic, should match TransactionDate)
        public DateTime CreatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        // ✅ When record was last modified
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public WorkOrder WorkOrder { get; set; } = null!;
        public ProcessStage ProcessStage { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;
        public User? UpdatedByUser { get; set; }

        // NEW: Shift-related properties
        public DateOnly ShiftDate { get; set; }
        
        
        public int ShiftScheduleId { get; set; }

        public ShiftType ShiftType { get; set; }

    }
}

