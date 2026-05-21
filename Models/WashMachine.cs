namespace wsahRecieveDelivary.Models
{
    public class WashMachine
    {
        public long Id { get; set; }
        public string MachineCode { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public bool IsDeleted { get; set; } 
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } 
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        
    }
}
