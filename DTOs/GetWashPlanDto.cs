using System.ComponentModel.DataAnnotations;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.DTOs
{
    public class GetWashPlanDto
    {
        public string? Plant { get; set; }
        public string? Unit { get; set; }
        public string? Buyer { get; set; }
        public string? StyleName { get; set; }
        public string? FastReactNo { get; set; }
        public string? Color { get; set; }
        public long? WorkOrderId { get; set; }
        public string? WorkOrderNo { get; set; }

        public decimal? OrderQuantity { get; set; }
        public DateTime? TOD { get; set; }

        public decimal? WashBalance { get; set; }
        public decimal? FromReceived { get; set; }

        public string? Marks { get; set; }

        public String? FirstWashBatchQty { get; set; }
        public String? FirstWashBatchTime { get; set; }

        public String? SecondWashBatchQty { get; set; }
        public String? SecondWashBatchTime { get; set; }
    }

    public class WashPlanModalFilterDto
    {
        public string? Search { get; set; }

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
