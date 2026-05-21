using Microsoft.EntityFrameworkCore;
using wsahRecieveDelivary.Data;
using wsahRecieveDelivary.DTOs;
using wsahRecieveDelivary.Models;

namespace wsahRecieveDelivary.Services
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthService(ApplicationDbContext context, IJwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if username exists
            if (await _context.Users.AnyAsync(u => u.Username == registerDto.Username))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Username already exists"
                };
            }

            // Check if email exists
            if (await _context.Users.AnyAsync(u => u.Email == registerDto.Email))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Email already exists"
                };
            }

            // Create new user
            var user = new User
            {
                FullName = registerDto.FullName,
                Username = registerDto.Username,
                Email = registerDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Assign roles
            if (registerDto.RoleIds.Any())
            {
                foreach (var roleId in registerDto.RoleIds)
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

            // Check if user is Admin
            var isAdmin = registerDto.RoleIds.Contains(1); // RoleId 1 = Admin

            // ✅ CHANGED: Assign ProcessStage access instead of Category
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
                if (registerDto.StageIds.Any())
                {
                    foreach (var stageId in registerDto.StageIds)
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

            return new AuthResponseDto
            {
                Success = true,
                Message = "User registered successfully"
            };
        }

        //public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        //{
        //    // Find user by username OR email
        //    var user = await _context.Users
        //        .Include(u => u.UserRoles)
        //            .ThenInclude(ur => ur.Role)
        //        .Include(u => u.UserProcessStageAccesses)
        //            .ThenInclude(upa => upa.ProcessStage)
        //        .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

        //    if (user == null)
        //    {
        //        return new AuthResponseDto
        //        {
        //            Success = false,
        //            Message = "Invalid username or password"
        //        };
        //    }

        //    // Check if user is active
        //    if (!user.IsActive)
        //    {
        //        return new AuthResponseDto
        //        {
        //            Success = false,
        //            Message = "User account is inactive"
        //        };
        //    }

        //    // Verify password
        //    if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        //    {
        //        return new AuthResponseDto
        //        {
        //            Success = false,
        //            Message = "Invalid username or password"
        //        };
        //    }

        //    // Get roles
        //    var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        //    var isAdmin = roles.Contains("Admin");

        //    // ✅ CHANGED: Get stages instead of categories
        //    List<string> stages;
        //    List<ProcessStageAccessDto> stageAccesses;

        //    if (isAdmin)
        //    {
        //        // Admin gets ALL stages
        //        var allStages = await _context.ProcessStages.Where(s => s.IsActive).ToListAsync();
        //        stages = allStages.Select(s => s.Name).ToList();
        //        stageAccesses = allStages.Select(s => new ProcessStageAccessDto
        //        {
        //            ProcessStageId = s.Id,
        //            ProcessStageName = s.Name,
        //            CanView = true,
        //            CanEdit = true,
        //            CanDelete = true
        //        }).ToList();
        //    }
        //    else
        //    {
        //        // User gets assigned stages only
        //        stages = user.UserProcessStageAccesses
        //            .Where(upa => upa.ProcessStage.IsActive)
        //            .Select(upa => upa.ProcessStage.Name)
        //            .ToList();

        //        stageAccesses = user.UserProcessStageAccesses
        //            .Where(upa => upa.ProcessStage.IsActive)
        //            .Select(upa => new ProcessStageAccessDto
        //            {
        //                ProcessStageId = upa.ProcessStageId,
        //                ProcessStageName = upa.ProcessStage.Name,
        //                CanView = upa.CanView,
        //                CanEdit = upa.CanEdit,
        //                CanDelete = upa.CanDelete
        //            }).ToList();
        //    }

        //    // Generate token
        //    var token = _jwtService.GenerateToken(user, roles, stages);

        //    return new AuthResponseDto
        //    {
        //        Success = true,
        //        Message = "Login successful",
        //        Token = token,
        //        User = new UserInfoDto
        //        {
        //            Id = user.Id,
        //            FullName = user.FullName,
        //            Username = user.Username,
        //            Email = user.Email,
        //            Roles = roles,
        //            ProcessStageAccesses = stageAccesses
        //        }
        //    };
        //}

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by username OR email
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserProcessStageAccesses)
                    .ThenInclude(upa => upa.ProcessStage)
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username || u.Email == loginDto.Username);

            if (user == null)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "User account is inactive"
                };
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                return new AuthResponseDto
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }

            // Get roles
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var isAdmin = roles.Contains("Admin");
            var isDashboardUser = roles.Contains("Incharge");

            // Get stages
            List<string> stages;
            List<ProcessStageAccessDto> stageAccesses;

            if (isAdmin)
            {
                // Admin gets ALL stages
                var allStages = await _context.ProcessStages.Where(s => s.IsActive).ToListAsync();
                stages = allStages.Select(s => s.Name).ToList();
                stageAccesses = allStages.Select(s => new ProcessStageAccessDto
                {
                    ProcessStageId = s.Id,
                    ProcessStageName = s.Name,
                    CanView = true,
                    CanEdit = true,
                    CanDelete = true
                }).ToList();
            }
            else
            {
                // User gets assigned stages only
                stages = user.UserProcessStageAccesses
                    .Where(upa => upa.ProcessStage.IsActive)
                    .Select(upa => upa.ProcessStage.Name)
                    .ToList();

                stageAccesses = user.UserProcessStageAccesses
                    .Where(upa => upa.ProcessStage.IsActive)
                    .Select(upa => new ProcessStageAccessDto
                    {
                        ProcessStageId = upa.ProcessStageId,
                        ProcessStageName = upa.ProcessStage.Name,
                        CanView = upa.CanView,
                        CanEdit = upa.CanEdit,
                        CanDelete = upa.CanDelete
                    }).ToList();
            }

            // Get user assigns
            List<UserAssignResponseDto> userAssigns;

            if (isAdmin)
            {
                // Admin gets ALL Plant/Unit combinations
                var allPlants = await _context.Plants.Where(p => !p.isDeleted).ToListAsync();
                var allUnits = await _context.Units.Where(u => !u.IsDeleted).ToListAsync();

                userAssigns = (from p in allPlants
                               from u in allUnits.Where(un => un.PlantId == p.Id)
                               select new UserAssignResponseDto
                               {
                                   PlantId = p.Id,
                                   UnitId = u.Id,
                                   PlantName = p.Name,
                                   UnitName = u.Name
                               }).ToList();
            }
            else if (isDashboardUser)
            {
                // DashboardUser gets assigned Plant/Unit combinations from UserAssign table
                var userAssignsDb = await (from ua in _context.UserAssigns
                                           join p in _context.Plants on ua.PlantId equals p.Id
                                           join u in _context.Units on ua.UnitId equals u.Id
                                           where ua.UserId == user.Id && !ua.isDeleted && !p.isDeleted && !u.IsDeleted
                                           select new { ua, p, u }).ToListAsync();

                userAssigns = userAssignsDb.Select(x => new UserAssignResponseDto
                {
                    PlantId = x.ua.PlantId,
                    UnitId = x.ua.UnitId,
                    PlantName = x.p.Name,
                    UnitName = x.u.Name
                }).ToList();
            }
            else
            {
                // Regular User (not DashboardUser) - no Plant/Unit access
                // DashboardUser gets assigned Plant/Unit combinations from UserAssign table
                var userAssignsDb = await (from ua in _context.UserAssigns
                                           join p in _context.Plants on ua.PlantId equals p.Id
                                           join u in _context.Units on ua.UnitId equals u.Id
                                           where ua.UserId == user.Id && !ua.isDeleted && !p.isDeleted && !u.IsDeleted
                                           select new { ua, p, u }).ToListAsync();

                userAssigns = userAssignsDb.Select(x => new UserAssignResponseDto
                {
                    PlantId = x.ua.PlantId,
                    UnitId = x.ua.UnitId,
                    PlantName = x.p.Name,
                    UnitName = x.u.Name
                }).ToList();
            }

            // Generate token
            var token = _jwtService.GenerateToken(user, roles, stages, userAssigns);

            return new AuthResponseDto
            {
                Success = true,
                Message = "Login successful",
                Token = token,
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Username = user.Username,
                    Email = user.Email,
                    Roles = roles,
                    ProcessStageAccesses = stageAccesses,
                    UserAssigns = userAssigns
                }
            };
        }
    }
}