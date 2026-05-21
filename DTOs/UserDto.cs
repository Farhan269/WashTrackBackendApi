// D:\test c#\wsahRecieveDelivary\DTOs\UserDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public List<ProcessStageAccessDto> ProcessStageAccesses { get; set; } = new List<ProcessStageAccessDto>();
        public List<UserAssignResponseDto> UserAssigns { get; set; } = new List<UserAssignResponseDto>();
    }
}