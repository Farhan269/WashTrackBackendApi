namespace wsahRecieveDelivary.DTOs
{
    public class TopIssueDTO
    {
        //public int PlantId { get; set; }
        //public string PlantName { get; set; }

        //public int UnitId { get; set; }
        //public string UnitName { get; set; }

        //public int ProcessModuleId { get; set; }
        //public string ProcessModuleName { get; set; }

        public int WashProcessId { get; set; }
        public string? ProcessName { get; set; }

        public int WashProcessIssueId { get; set; }
        public string? IssueName { get; set; }

        //public DateTime OperationalDate { get; set; }
        //public int Shift { get; set; }

        public decimal IssueQty { get; set; }
    }
}
