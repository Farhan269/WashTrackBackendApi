// D:\test c#\wsahRecieveDelivary\DTOs\AssignStagesDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class AssignStagesDto
    {
        public List<int> StageIds { get; set; } = new List<int>();
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
    }
}