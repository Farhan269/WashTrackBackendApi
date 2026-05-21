namespace wsahRecieveDelivary.DTOs
{
    public class CreateWashPlanDto
    {
        public int WorkOrderId { get; set; }
        public int ProcessStageId { get; set; }
        public DateOnly PlanDate { get; set; }
        public int Shift { get; set; }
        public int PlantId { get; set; }
        public int UnitId { get; set; }
        // 🔥 MULTIPLE MACHINES
        public List<long>? MachineIds { get; set; }
        // 🔹 NEW FIELDS
        public decimal BaseTargetQty { get; set; }
        public decimal Percentage { get; set; }
        public decimal FinalTargetQty { get; set; }

        // optional (if you keep it)
        public decimal? AdjustedTargetQty { get; set; }
        public string? Remarks { get; set; }
        public bool IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
