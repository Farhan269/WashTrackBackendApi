using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Services
{
    public interface IJwtService
    {
     //   string GenerateToken(User user, List<string> roles, List<string> categories);
        string GenerateToken(User user, List<string> roles, List<string> stages, List<UserAssignResponseDto> userAssigns);
    }
}