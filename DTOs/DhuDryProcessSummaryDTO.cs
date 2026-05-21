namespace wsahRecieveDelivary.DTOs
{
    public class DhuDryProcessSummaryDTO
    {
        //public int PlantId { get; set; }

        //public string PlantName { get; set; }

        //public int UnitId { get; set; }

        //public string UnitName { get; set; }

        public int ProcessModuleId { get; set; }

        public string ProcessModuleName { get; set; }

        public int WashProcessId { get; set; }

        public string? ProcessName { get; set; }
        //public int? Shift { get; set; }
        public decimal PassQty { get; set; }

        public decimal DefectQty { get; set; }

        public decimal RejectQty { get; set; }

        public decimal IssueQty { get; set; }

        public decimal DayTarget { get; set; }

        public decimal ManPower { get; set; }

        public decimal SMV { get; set; }

        public decimal DHU { get; set; }

        public decimal PlanEff { get; set; }

        public decimal ActualEff { get; set; }
    }

    public class DryProcessSummaryFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public List<int>? PlantId { get; set; }
        public List<int>? UnitId { get; set; }
        public List<int>? ProcessModuleId { get; set; }
        public List<int>? WashProcessId { get; set; }

        public List<int>? Shift { get; set; } // 1 = Day, 2 = Night
    }
}
