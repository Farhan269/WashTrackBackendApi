namespace wsahRecieveDelivary.Models
{
    public class WashMachineAssign
    {
        public long Id { get; set; }
        public long MachineId { get; set; }
        public int PlantId { get; set; }
        public int UnitId { get; set; }
        public bool IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
