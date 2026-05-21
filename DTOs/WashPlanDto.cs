namespace wsahRecieveDelivary.DTOs
{
    public class WashPlanDto
    {
        public long Id { get; set; }
        public int WorkOrderId { get; set; }
        public string WorkOrderNo { get; set; }

        public DateOnly PlanDate { get; set; }

        public int PlantId { get; set; }
        public string PlantName { get; set; }

        public int UnitId { get; set; }
        public string UnitName { get; set; }

        public int Shift { get; set; }

        public string Factory { get; set; }
        public string Line { get; set; }

        public string Buyer { get; set; }
        public string BuyerDepartment { get; set; }
        public string StyleName { get; set; }
        public string FastReactNo { get; set; }
        public string Color { get; set; }
        public string WashType { get; set; }

        public decimal OrderQuantity { get; set; }
        public decimal CutQty { get; set; }
        public DateTime? TOD { get; set; }

        public DateTime? SewingCompDate { get; set; }
        public DateTime? FirstRCVDate { get; set; }
        public DateTime? WashApprovalDate { get; set; }
        public DateTime? WashTargetDate { get; set; }

        public decimal TotalWashReceived { get; set; }
        public decimal TotalWashDelivery { get; set; }
        public decimal WashBalance { get; set; }

        public string Marks { get; set; }

        public int ProcessStageId { get; set; }
        public string ProcessStageName { get; set; }

        public List<MachineDto> Machines { get; set; }
     

        public decimal FinalTargetQty { get; set; }
        public decimal? BaseTargetQty { get; set; }
    }
}
