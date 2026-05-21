namespace wsahRecieveDelivary.DTOs
{
    public class DashboardDto
    {
        // Response metadata
        public DateOnly ShiftDate { get; set; }
        public string Plant { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string ShiftType { get; set; } = string.Empty;
        public int ProcessStageId { get; set; }
        public string ProcessStageName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public long TotalQuantity { get; set; }
    }
}
