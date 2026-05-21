namespace wsahRecieveDelivary.Models
{
    public class UserAssign
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? PlantId { get; set; } 
        public int? UnitId { get; set; } 
        public bool isDeleted { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }



    }
}
