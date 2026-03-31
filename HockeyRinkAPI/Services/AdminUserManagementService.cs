using HockeyRinkAPI.Models;
using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HockeyRinkAPI.Services;

public class AdminUserManagementService : IAdminUserManagementService
{
    private const string SubAdminRole = "SubAdmin";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AdminUserManagementService> _logger;

    public AdminUserManagementService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AdminUserManagementService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<List<AdminUserSummaryDto>> GetAllUsersWithPermissionsAsync()
    {
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var subAdmins = await _userManager.GetUsersInRoleAsync(SubAdminRole);

        var allAdminUsers = admins.Union(subAdmins, UserEqualityComparer.Instance).ToList();

        var result = new List<AdminUserSummaryDto>();
        foreach (var user in allAdminUsers)
        {
            var claims = await _userManager.GetClaimsAsync(user);
            var permissions = claims
                .Where(c => c.Type == AdminPermissions.ClaimType)
                .Select(c => c.Value)
                .ToList();

            result.Add(new AdminUserSummaryDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                IsFullAdmin = await _userManager.IsInRoleAsync(user, "Admin"),
                IsSubAdmin = await _userManager.IsInRoleAsync(user, SubAdminRole),
                Permissions = permissions,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
            });
        }

        return result.OrderBy(u => u.IsFullAdmin ? 0 : 1).ThenBy(u => u.Email).ToList();
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string? search, int take = 50)
    {
        var query = _userManager.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(u =>
                (u.FirstName + " " + u.LastName).ToLower().Contains(lower) ||
                u.Email!.ToLower().Contains(lower));
        }

        return await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(take)
            .Select(u => new UserSearchResultDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
            })
            .ToListAsync();
    }

    public async Task<OperationResult> GrantAdminAsync(string userId, IEnumerable<string> permissions)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return OperationResult.Fail("User is already a full admin");

        await EnsureSubAdminRoleExistsAsync();

        if (!await _userManager.IsInRoleAsync(user, SubAdminRole))
        {
            var addRole = await _userManager.AddToRoleAsync(user, SubAdminRole);
            if (!addRole.Succeeded)
                return OperationResult.Fail(string.Join(", ", addRole.Errors.Select(e => e.Description)));
        }

        await SetPermissionClaimsAsync(user, permissions);

        _logger.LogInformation("Granted sub-admin access to user {UserId} ({Email})", user.Id, user.Email);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> UpdatePermissionsAsync(string userId, IEnumerable<string> permissions)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return OperationResult.Fail("Cannot modify permissions of a full admin");

        await SetPermissionClaimsAsync(user, permissions);

        _logger.LogInformation("Updated permissions for sub-admin {UserId} ({Email})", user.Id, user.Email);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> RevokeAdminAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return OperationResult.Fail("User not found");

        if (await _userManager.IsInRoleAsync(user, "Admin"))
            return OperationResult.Fail("Cannot revoke a full admin through this endpoint");

        if (await _userManager.IsInRoleAsync(user, SubAdminRole))
        {
            var removeRole = await _userManager.RemoveFromRoleAsync(user, SubAdminRole);
            if (!removeRole.Succeeded)
                return OperationResult.Fail(string.Join(", ", removeRole.Errors.Select(e => e.Description)));
        }

        // Remove all permission claims
        var existingClaims = (await _userManager.GetClaimsAsync(user))
            .Where(c => c.Type == AdminPermissions.ClaimType)
            .ToList();

        if (existingClaims.Count > 0)
            await _userManager.RemoveClaimsAsync(user, existingClaims);

        _logger.LogInformation("Revoked sub-admin access from user {UserId} ({Email})", user.Id, user.Email);
        return OperationResult.Ok();
    }

    public async Task<OperationResult> InviteUserAsync(string email, IEnumerable<string> permissions)
    {
        var permList = permissions.ToList();
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing != null)
        {
            // Promote existing user
            return await GrantAdminAsync(existing.Id, permList);
        }

        await EnsureSubAdminRoleExistsAsync();

        var newUser = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = false,
            IsManuallyRegistered = true,
            PasswordSetupToken = Guid.NewGuid().ToString(),
            PasswordSetupTokenExpiry = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
            return OperationResult.Fail(string.Join(", ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(newUser, SubAdminRole);
        await SetPermissionClaimsAsync(newUser, permList);

        // TODO: Send invitation email with password setup link using newUser.PasswordSetupToken
        _logger.LogInformation(
            "Invited new sub-admin {Email} with setup token {Token}",
            email, newUser.PasswordSetupToken);

        return OperationResult.Ok();
    }

    private async Task SetPermissionClaimsAsync(ApplicationUser user, IEnumerable<string> permissions)
    {
        // Remove all existing permission claims
        var existing = (await _userManager.GetClaimsAsync(user))
            .Where(c => c.Type == AdminPermissions.ClaimType)
            .ToList();
        if (existing.Count > 0)
            await _userManager.RemoveClaimsAsync(user, existing);

        // Add new permission claims (only valid ones)
        var validPerms = permissions.Intersect(AdminPermissions.All).ToList();
        if (validPerms.Count > 0)
        {
            var newClaims = validPerms.Select(p => new Claim(AdminPermissions.ClaimType, p)).ToList();
            await _userManager.AddClaimsAsync(user, newClaims);
        }
    }

    private async Task EnsureSubAdminRoleExistsAsync()
    {
        if (!await _roleManager.RoleExistsAsync(SubAdminRole))
            await _roleManager.CreateAsync(new IdentityRole(SubAdminRole));
    }

    private sealed class UserEqualityComparer : IEqualityComparer<ApplicationUser>
    {
        public static readonly UserEqualityComparer Instance = new();
        public bool Equals(ApplicationUser? x, ApplicationUser? y) => x?.Id == y?.Id;
        public int GetHashCode(ApplicationUser obj) => obj.Id.GetHashCode();
    }
}
