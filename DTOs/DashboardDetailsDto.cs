namespace wsahRecieveDelivary.DTOs
{
    public class DashboardDetailsDto
    {
        public string Factory { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string WorkOrderNo { get; set; } = string.Empty;
        public string? BuyerDepartment { get; set; }
        public string StyleName { get; set; } = string.Empty;
        public string? FastReactNo { get; set; }
        public decimal OrderQuantity { get; set; }
        public DateTime? WashTargetDate { get; set; }
        public decimal TotalWashReceived { get; set; }
        public decimal TotalWashDelivery { get; set; }
        public string ShiftType { get; set; } = string.Empty;
        public string StageName { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }

    public class DashboardDetailsResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<DashboardDetailsDto> Data { get; set; } = new();
        public PaginationMetadata Pagination { get; set; } = new();
    }
}
