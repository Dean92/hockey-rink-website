using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;

namespace HockeyRinkAPI.Services;

public interface IAdminUserManagementService
{
    Task<List<AdminUserSummaryDto>> GetAllUsersWithPermissionsAsync();
    Task<List<UserSearchResultDto>> SearchUsersAsync(string? search, int take = 50);
    Task<OperationResult> GrantAdminAsync(string userId, IEnumerable<string> permissions);
    Task<OperationResult> UpdatePermissionsAsync(string userId, IEnumerable<string> permissions);
    Task<OperationResult> RevokeAdminAsync(string userId);
    Task<OperationResult> InviteUserAsync(string email, IEnumerable<string> permissions);
}
