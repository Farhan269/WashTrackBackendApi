using System.ComponentModel.DataAnnotations;

namespace wsahRecieveDelivary.DTOs
{
    public class RegisterDto
    {
        [Required]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        public List<int> RoleIds { get; set; } = new List<int>();

        // ✅ CHANGED: StageIds instead of CategoryIds
        public List<int> StageIds { get; set; } = new List<int>();
    }
}