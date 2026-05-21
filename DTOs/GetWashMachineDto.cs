namespace wsahRecieveDelivary.DTOs
{
    public class GetWashMachineDto
    {
        public long Id { get; set; }
        public string MachineCode { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string PlantName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
    }
}
