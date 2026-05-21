namespace wsahRecieveDelivary.DTOs
{
    public class WashPlanFilterDto
    {
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }

        public int? PlantId { get; set; }
        public int? UnitId { get; set; }
        public int? Shift { get; set; }
        public int? ProcessStageId { get; set; }

        public string? Search { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
