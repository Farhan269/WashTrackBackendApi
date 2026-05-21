namespace wsahRecieveDelivary.DTOs
{
    public class AuthResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Token { get; set; }
        public UserInfoDto? User { get; set; }
    }

    public class UserInfoDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new List<string>();
        public List<ProcessStageAccessDto> ProcessStageAccesses { get; set; } = new List<ProcessStageAccessDto>();
        public List<UserAssignResponseDto> UserAssigns { get; set; } = new List<UserAssignResponseDto>();
    }

    // ✅ CHANGED: ProcessStageAccessDto instead of CategoryAccessDto
    public class ProcessStageAccessDto
    {
        public int ProcessStageId { get; set; }
        public string ProcessStageName { get; set; } = string.Empty;
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }
}