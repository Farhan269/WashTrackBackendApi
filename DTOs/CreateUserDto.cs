// D:\test c#\wsahRecieveDelivary\DTOs\CreateUserDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class CreateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public List<int> RoleIds { get; set; } = new List<int>();
        public List<int> StageIds { get; set; } = new List<int>();
    }
}