// D:\test c#\wsahRecieveDelivary\DTOs\UserListResponseDto.cs
namespace wsahRecieveDelivary.DTOs
{
    public class UserListResponseDto
    {
        public List<UserDto> Users { get; set; } = new List<UserDto>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}