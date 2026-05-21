// D:\test c#\wsahRecieveDelivary\Services\IUserService.cs
using wsahRecieveDelivary.DTOs;

namespace wsahRecieveDelivary.Services
{
    public interface IUserService
    {
        Task<ApiResponse<UserListResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize);
        Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
        Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto);
        Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task<ApiResponse<bool>> DeleteUserAsync(int id);
        Task<ApiResponse<UserDto>> AssignRolesToUserAsync(int id, AssignRolesDto assignRolesDto);
        Task<ApiResponse<UserDto>> AssignStagesToUserAsync(int id, AssignStagesDto assignStagesDto);
        Task<ApiResponse<UserDto>> ToggleUserStatusAsync(int id);

        Task<MessageHelper> AssignUser(UserAssignDto obj);
    }
}