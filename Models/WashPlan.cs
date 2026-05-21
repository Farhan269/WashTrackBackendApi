namespace wsahRecieveDelivary.Models
{
    public class WashPlan
    {
        public long Id { get; set; }
        public int WorkOrderId { get; set; }
        public int ProcessStageId { get; set; }
        public DateOnly PlanDate { get; set; }
        public int Shift { get; set; }
        public int PlantId { get; set; }
        public int UnitId { get; set; }
        public decimal? BaseTargetQty { get; set; }
        public decimal? Percentage { get; set; }
        public decimal? AdjustedTargetQty { get; set; }
        public decimal? FinalTargetQty { get; set; }
        public string Remarks { get; set; }
        public bool IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
