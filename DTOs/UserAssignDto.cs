namespace wsahRecieveDelivary.DTOs
{
    public class UserAssignDto
    {
        public int UserId { get; set; }
        public List<UserAssignmentDetail>? UserAssignments { get; set; }
        public int? CreatedBy { get; set; }

        // NEW
        public List<int>? ProcessStageIds { get; set; }

        public bool? CanEdit { get; set; }
        public bool? CanDelete { get; set; }


    }

    public class UserAssignmentDetail
    {
        public int? PlantId { get; set; }
        public int? UnitId { get; set; }
    }
    public class UserAssignResponseDto
    {
        public int? PlantId { get; set; }
        public int? UnitId { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
    }
    public class MessageHelper
    {
        public string Message { get; set; } = string.Empty;
    }
}

