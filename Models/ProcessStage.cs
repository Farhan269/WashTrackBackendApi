using System.ComponentModel.DataAnnotations;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Models
{
    public class ProcessStage
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public ICollection<WashTransaction> WashTransactions { get; set; } = new List<WashTransaction>();
        public ICollection<ProcessStageBalance> ProcessStageBalances { get; set; } = new List<ProcessStageBalance>();
        public ICollection<UserProcessStageAccess> UserProcessStageAccesses { get; set; } = new List<UserProcessStageAccess>();
    }
}