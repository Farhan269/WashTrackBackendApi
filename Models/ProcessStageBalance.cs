namespace wsahRecieveDelivary.Models
{
    public class ProcessStageBalance
    {
        public int Id { get; set; }
        public int WorkOrderId { get; set; }

        // ✅ CHANGED: From enum to foreign key
        public int ProcessStageId { get; set; }

        public int TotalReceived { get; set; }
        public int TotalDelivered { get; set; }
        public int CurrentBalance { get; set; }

        public DateTime? LastReceiveDate { get; set; }
        public DateTime? LastDeliveryDate { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public WorkOrder WorkOrder { get; set; } = null!;
        public ProcessStage ProcessStage { get; set; } = null!;
    }
}