// D:\test c#\wsahRecieveDelivary\DTOs\UpdateUserDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class UpdateUserDto
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public List<int> StageIds { get; set; } = new List<int>();   
        public List<UpdateStageAccessDto> StageAccesses { get; set; } = new List<UpdateStageAccessDto>();
    }
}