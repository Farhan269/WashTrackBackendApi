// D:\test c#\wsahRecieveDelivary\DTOs\UpdateStageAccessDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class UpdateStageAccessDto
    {
        public int ProcessStageId { get; set; }
        public bool CanView { get; set; } = true;
        public bool CanEdit { get; set; } = false;
        public bool CanDelete { get; set; } = false;
    }
}