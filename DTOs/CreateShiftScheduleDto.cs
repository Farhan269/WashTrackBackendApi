namespace wsahRecieveDelivary.DTOs
{
    public class CreateShiftScheduleDto
    {
        public string Name { get; set; } = string.Empty;
        public TimeSpan DayShiftStart { get; set; }
        public TimeSpan DayShiftEnd { get; set; }
        public TimeSpan NightShiftStart { get; set; }
        public TimeSpan NightShiftEnd { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }
    }
}
