namespace wsahRecieveDelivary.Models
{
    public class UserProcessStageAccess
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ProcessStageId { get; set; }

        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public User User { get; set; } = null!;
        public ProcessStage ProcessStage { get; set; } = null!;
    }
}