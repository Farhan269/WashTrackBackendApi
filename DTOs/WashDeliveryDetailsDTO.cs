namespace wsahRecieveDelivary.DTOs
{
    public class WashDeliveryDetailsDTO
    {
        public DateTime? ProductionDate { get; set; }

        public string? Factory { get; set; }
        public string? Unit { get; set; }
        public string? Buyer { get; set; }
        public string? WorkOrderNo { get; set; }
        public string? StyleName { get; set; }
        public string? FastReactNo { get; set; }
        public string? Color { get; set; }

        public decimal OrderQuantity { get; set; }

        public DateTime? WashTargetDate { get; set; }
        public DateTime? TOD { get; set; }

        public decimal TotalWashReceived { get; set; }
        public decimal TotalWashDelivery { get; set; }
        public decimal Receive { get; set; }

        public decimal Delivery { get; set; }
    }
    public class PaginatedResponseDTO<T>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
