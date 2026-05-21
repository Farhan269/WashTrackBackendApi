namespace wsahRecieveDelivary.Models
{
    public class ShiftSchedule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public TimeSpan DayShiftStart { get; set; }
        public TimeSpan DayShiftEnd { get; set; }
        public TimeSpan NightShiftStart { get; set; }
        public TimeSpan NightShiftEnd { get; set; }
        public DateTime EffectiveFromDate { get; set; }
        public DateTime? EffectiveToDate { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
    }
}
