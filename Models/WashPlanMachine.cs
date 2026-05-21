using Microsoft.AspNetCore.Http.HttpResults;

namespace wsahRecieveDelivary.Models
{
    public class WashPlanMachine
    {
        public long Id { get; set; }
        public long WashPlanId { get; set; }
        public long MachineId { get; set; }
        public bool IsDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

