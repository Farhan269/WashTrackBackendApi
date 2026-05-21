// D:\test c#\wsahRecieveDelivary\Services\UserService.cs
using Microsoft.EntityFrameworkCore;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context)
        {
            _context = context;
        }

        //public async Task<ApiResponse<UserListResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        //{
        //    try
        //    {
        //        var totalCount = await _context.Users.CountAsync();
        //        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        //        var users = await _context.Users
        //            .Include(u => u.UserRoles)
        //                .ThenInclude(ur => ur.Role)
        //            .Include(u => u.UserProcessStageAccesses)
        //                .ThenInclude(upa => upa.ProcessStage)

        //            .Skip((pageNumber - 1) * pageSize)
        //            .Take(pageSize)
        //            .ToListAsync();

        //        var userDtos = users.Select(u => MapToUserDto(u)).ToList();



        //        return new ApiResponse<UserListResponseDto>
        //        {
        //            Success = true,
        //            Message = "Users retrieved successfully",
        //            Data = new UserListResponseDto
        //            {
        //                Users = userDtos,
        //                TotalCount = totalCount,
        //                PageNumber = pageNumber,
        //                PageSize = pageSize,
        //                TotalPages = totalPages
        //            }
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<UserListResponseDto>
        //        {
        //            Success = false,
        //            Message = $"Error retrieving users: {ex.Message}"
        //        };
        //    }
        //}

        public async Task<ApiResponse<UserListResponseDto>> GetAllUsersAsync(int pageNumber, int pageSize)
        {
            try
            {
                var totalCount = await _context.Users.CountAsync();
                var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

                var users = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var userIds = users.Select(u => u.Id).ToList();
                var userAssigns = await (from ua in _context.Set<UserAssign>()
                                         where userIds.Contains(ua.UserId) && !ua.isDeleted && ua.PlantId != null && ua.UnitId != null
                                         join p in _context.Plants on ua.PlantId equals p.Id
                                         join un in _context.Units on ua.UnitId equals un.Id
                                         select new
                                         {
                                             ua.UserId,
                                             ua.PlantId,
                                             ua.UnitId,
                                             PlantName = p.Name,
                                             UnitName = un.Name
                                         }).ToListAsync();

                var userDtos = users.Select(u =>
                {
                    var userDto = MapToUserDto(u);
                    userDto.UserAssigns = userAssigns
                        .Where(ua => ua.UserId == u.Id)
                        .Select(ua => new UserAssignResponseDto
                        {
                            PlantId = ua.PlantId,
                            UnitId = ua.UnitId,
                            PlantName = ua.PlantName,
                            UnitName = ua.UnitName
                        }).ToList();
                    return userDto;
                }).ToList();

                return new ApiResponse<UserListResponseDto>
                {
                    Success = true,
                    Message = "Users retrieved successfully",
                    Data = new UserListResponseDto
                    {
                        Users = userDtos,
                        TotalCount = totalCount,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalPages = totalPages
                    }
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserListResponseDto>
                {
                    Success = false,
                    Message = $"Error retrieving users: {ex.Message}"
                };
            }
        }

        //public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        //{
        //    try
        //    {
        //        var user = await _context.Users
        //            .Include(u => u.UserRoles)
        //                .ThenInclude(ur => ur.Role)
        //            .Include(u => u.UserProcessStageAccesses)
        //                .ThenInclude(upa => upa.ProcessStage)
        //            .FirstOrDefaultAsync(u => u.Id == id);

        //        if (user == null)
        //        {
        //            return new ApiResponse<UserDto>
        //            {
        //                Success = false,
        //                Message = "User not found"
        //            };
        //        }



        //        return new ApiResponse<UserDto>
        //        {
        //            Success = true,
        //            Message = "User retrieved successfully",
        //            Data = MapToUserDto(user)
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        return new ApiResponse<UserDto>
        //        {
        //            Success = false,
        //            Message = $"Error retrieving user: {ex.Message}"
        //        };
        //    }
        //}


        public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var userDto = MapToUserDto(user);

                var userAssigns = await (from ua in _context.Set<UserAssign>()
                                         where ua.UserId == id && !ua.isDeleted && ua.PlantId != null && ua.UnitId != null
                                         join p in _context.Plants on ua.PlantId equals p.Id
                                         join un in _context.Units on ua.UnitId equals un.Id
                                         select new UserAssignResponseDto
                                         {
                                             PlantId = ua.PlantId,
                                             UnitId = ua.UnitId,
                                             PlantName = p.Name,
                                             UnitName = un.Name
                                         }).ToListAsync();

                userDto.UserAssigns = userAssigns;

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User retrieved successfully",
                    Data = userDto
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error retrieving user: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto)
        {
            try
            {
                // Check if username exists
                if (await _context.Users.AnyAsync(u => u.Username == createUserDto.Username))
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Username already exists"
                    };
                }

                // Check if email exists
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Email already exists"
                    };
                }

                // Create new user
                var user = new User
                {
                    FullName = createUserDto.FullName,
                    Username = createUserDto.Username,
                    Email = createUserDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Assign roles
                if (createUserDto.RoleIds.Any())
                {
                    foreach (var roleId in createUserDto.RoleIds)
                    {
                        _context.UserRoles.Add(new UserRole
                        {
                            UserId = user.Id,
                            RoleId = roleId
                        });
                    }
                }
                else
                {
                    // Default role: User (RoleId = 2)
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = 2
                    });
                }

                await _context.SaveChangesAsync();

                // Assign stages
                var isAdmin = createUserDto.RoleIds.Contains(1); // RoleId 1 = Admin

                if (isAdmin)
                {
                    // Admin gets ALL stages automatically
                    var allStages = await _context.ProcessStages.Where(s => s.IsActive).ToListAsync();
                    foreach (var stage in allStages)
                    {
                        _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                        {
                            UserId = user.Id,
                            ProcessStageId = stage.Id,
                            CanView = true,
                            CanEdit = true,
                            CanDelete = true
                        });
                    }
                }
                else
                {
                    // User gets specific stages only
                    if (createUserDto.StageIds.Any())
                    {
                        foreach (var stageId in createUserDto.StageIds)
                        {
                            _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                            {
                                UserId = user.Id,
                                ProcessStageId = stageId,
                                CanView = true,
                                CanEdit = false,
                                CanDelete = false
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // Reload user with relations
                var createdUser = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User created successfully",
                    Data = MapToUserDto(createdUser!)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error creating user: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Check if new username exists (if being changed)
                if (!string.IsNullOrEmpty(updateUserDto.Username) && updateUserDto.Username != user.Username)
                {
                    if (await _context.Users.AnyAsync(u => u.Username == updateUserDto.Username && u.Id != id))
                    {
                        return new ApiResponse<UserDto>
                        {
                            Success = false,
                            Message = "Username already exists"
                        };
                    }
                    user.Username = updateUserDto.Username;
                }

                // Check if new email exists (if being changed)
                if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
                {
                    if (await _context.Users.AnyAsync(u => u.Email == updateUserDto.Email && u.Id != id))
                    {
                        return new ApiResponse<UserDto>
                        {
                            Success = false,
                            Message = "Email already exists"
                        };
                    }
                    user.Email = updateUserDto.Email;
                }

                if (!string.IsNullOrEmpty(updateUserDto.FullName))
                    user.FullName = updateUserDto.FullName;

                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
                }

                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                // ✅ UPDATE STAGE ACCESS - TWO OPTIONS

                // Option 1: If stageIds provided (replaces all stages)
                if (updateUserDto.StageIds.Any())
                {
                    var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
                    if (!isAdmin)
                    {
                        // Remove all existing stage accesses
                        var existingAccesses = await _context.UserProcessStageAccesses
                            .Where(upa => upa.UserId == id)
                            .ToListAsync();
                        _context.UserProcessStageAccesses.RemoveRange(existingAccesses);

                        // Add new stages
                        foreach (var stageId in updateUserDto.StageIds)
                        {
                            _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                            {
                                UserId = user.Id,
                                ProcessStageId = stageId,
                                CanView = true,
                                CanEdit = false,
                                CanDelete = false
                            });
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                // Option 2: If stageAccesses provided (updates specific permissions)
                else if (updateUserDto.StageAccesses.Any())
                {
                    var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
                    if (!isAdmin)
                    {
                        foreach (var stageAccessDto in updateUserDto.StageAccesses)
                        {
                            var stageAccess = await _context.UserProcessStageAccesses
                                .FirstOrDefaultAsync(upa => upa.UserId == id && upa.ProcessStageId == stageAccessDto.ProcessStageId);

                            if (stageAccess != null)
                            {
                                stageAccess.CanView = stageAccessDto.CanView;
                                stageAccess.CanEdit = stageAccessDto.CanEdit;
                                stageAccess.CanDelete = stageAccessDto.CanDelete;
                                _context.UserProcessStageAccesses.Update(stageAccess);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }

                // Reload user
                user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "User updated successfully",
                    Data = MapToUserDto(user!)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error updating user: {ex.Message}"
                };
            }
        }
        public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return new ApiResponse<bool>
                {
                    Success = true,
                    Message = "User deleted successfully",
                    Data = true
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = $"Error deleting user: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> AssignRolesToUserAsync(int id, AssignRolesDto assignRolesDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Remove existing roles
                var existingRoles = await _context.UserRoles.Where(ur => ur.UserId == id).ToListAsync();
                _context.UserRoles.RemoveRange(existingRoles);

                // Add new roles
                foreach (var roleId in assignRolesDto.RoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        UserId = user.Id,
                        RoleId = roleId
                    });
                }

                // If user is now Admin, give access to all stages
                if (assignRolesDto.RoleIds.Contains(1))
                {
                    var existingStages = await _context.UserProcessStageAccesses
                        .Where(upa => upa.UserId == id)
                        .ToListAsync();
                    _context.UserProcessStageAccesses.RemoveRange(existingStages);

                    var allStages = await _context.ProcessStages.Where(s => s.IsActive).ToListAsync();
                    foreach (var stage in allStages)
                    {
                        _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                        {
                            UserId = user.Id,
                            ProcessStageId = stage.Id,
                            CanView = true,
                            CanEdit = true,
                            CanDelete = true
                        });
                    }
                }

                await _context.SaveChangesAsync();

                // Reload user
                user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Roles assigned successfully",
                    Data = MapToUserDto(user!)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error assigning roles: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> AssignStagesToUserAsync(int id, AssignStagesDto assignStagesDto)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Check if user is Admin
                var isAdmin = user.UserRoles.Any(ur => ur.Role.Name == "Admin");
                if (isAdmin)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Cannot assign stages to Admin users"
                    };
                }

                // Remove existing stage accesses
                var existingAccesses = await _context.UserProcessStageAccesses
                    .Where(upa => upa.UserId == id)
                    .ToListAsync();
                _context.UserProcessStageAccesses.RemoveRange(existingAccesses);

                // Add new stage accesses
                foreach (var stageId in assignStagesDto.StageIds)
                {
                    _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                    {
                        UserId = user.Id,
                        ProcessStageId = stageId,
                        CanView = true,
                        CanEdit = assignStagesDto.CanEdit,
                        CanDelete = assignStagesDto.CanDelete
                    });
                }

                await _context.SaveChangesAsync();

                // Reload user
                user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = "Stages assigned successfully",
                    Data = MapToUserDto(user!)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error assigning stages: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<UserDto>> ToggleUserStatusAsync(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                    .Include(u => u.UserProcessStageAccesses)
                        .ThenInclude(upa => upa.ProcessStage)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.UtcNow;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                return new ApiResponse<UserDto>
                {
                    Success = true,
                    Message = $"User status changed to {(user.IsActive ? "Active" : "Inactive")}",
                    Data = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = $"Error toggling user status: {ex.Message}"
                };
            }
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                ProcessStageAccesses = user.UserProcessStageAccesses.Select(upa => new ProcessStageAccessDto
                {
                    ProcessStageId = upa.ProcessStageId,
                    ProcessStageName = upa.ProcessStage.Name,
                    CanView = upa.CanView,
                    CanEdit = upa.CanEdit,
                    CanDelete = upa.CanDelete
                }).ToList()
            };
        }


        public async Task<MessageHelper> AssignUser(UserAssignDto obj)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .FirstOrDefaultAsync(x => x.Id == obj.UserId && x.IsActive);

                if (user == null)
                {
                    return new MessageHelper { Message = "User not found." };
                }

                var roleNames = user.UserRoles.Select(r => r.Role.Name).ToList();

                var isAdmin = roleNames.Contains("Admin");
                var isPlanner = roleNames.Contains("Planner");

                if (isAdmin)
                {
                    return new MessageHelper { Message = "Cannot assign data to Admin user." };
                }

                // ==================================
                // 1. UNIT / PLANT ASSIGNMENT (ALL NON-ADMIN ROLES INCLUDING PLANNER)
                // ==================================
                if (obj.UserAssignments != null && obj.UserAssignments.Any())
                {
                    var existingAssigns = await _context.Set<UserAssign>()
                        .Where(ua => ua.UserId == obj.UserId)
                        .ToListAsync();

                    _context.Set<UserAssign>().RemoveRange(existingAssigns);

                    foreach (var assignment in obj.UserAssignments)
                    {
                        _context.Set<UserAssign>().Add(new UserAssign
                        {
                            UserId = obj.UserId,
                            PlantId = assignment.PlantId,
                            UnitId = assignment.UnitId,
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = obj.CreatedBy ?? 0,
                            UpdatedAt = DateTime.UtcNow,
                            UpdatedBy = obj.CreatedBy ?? 0,
                            isDeleted = false
                        });
                    }
                }

                // ==================================
                // 2. PROCESS STAGE ASSIGNMENT (PLANNER ONLY)
                // ==================================
                if (isPlanner && obj.ProcessStageIds != null && obj.ProcessStageIds.Any())
                {
                    var existingStages = await _context.UserProcessStageAccesses
                        .Where(x => x.UserId == obj.UserId)
                        .ToListAsync();

                    _context.UserProcessStageAccesses.RemoveRange(existingStages);

                    foreach (var stageId in obj.ProcessStageIds)
                    {
                        _context.UserProcessStageAccesses.Add(new UserProcessStageAccess
                        {
                            UserId = obj.UserId,
                            ProcessStageId = stageId,
                            CanView = true,
                            CanEdit = obj.CanEdit ?? false,
                            CanDelete = obj.CanDelete ?? false
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new MessageHelper
                {
                    Message = "User assignments updated successfully."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new MessageHelper
                {
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        //public async Task<MessageHelper> AssignUser(UserAssignDto obj)
        //{
        //    using var transaction = await _context.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var msg = new MessageHelper();

        //        var user = await _context.Users
        //            .FirstOrDefaultAsync(x => x.Id == obj.UserId && x.IsActive);

        //        if (user == null)
        //        {
        //            return new MessageHelper { Message = "User not found." };
        //        }

        //        if (obj.UserAssignments != null && obj.UserAssignments.Any())
        //        {
        //            var existingAssigns = await _context.Set<UserAssign>()
        //                .Where(ua => ua.UserId == obj.UserId)
        //                .ToListAsync();

        //            _context.Set<UserAssign>().RemoveRange(existingAssigns);

        //            foreach (var assignment in obj.UserAssignments)
        //            {
        //                _context.Set<UserAssign>().Add(new UserAssign
        //                {
        //                    UserId = obj.UserId,
        //                    PlantId = assignment.PlantId,
        //                    UnitId = assignment.UnitId,
        //                    CreatedAt = DateTime.UtcNow,
        //                    CreatedBy = obj.CreatedBy ?? 0,
        //                    isDeleted = false,
        //                    UpdatedAt = DateTime.UtcNow,
        //                    UpdatedBy = obj.CreatedBy ?? 0
        //                });
        //            }
        //        }

        //        await _context.SaveChangesAsync();
        //        await transaction.CommitAsync();

        //        msg.Message = "User assignments created successfully.";
        //        return msg;
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        return new MessageHelper { Message = $"Error: {ex.Message}" };
        //    }
        //}
    }
}