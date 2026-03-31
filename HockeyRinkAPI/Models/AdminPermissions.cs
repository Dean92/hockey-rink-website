namespace HockeyRinkAPI.Models;

/// <summary>
/// Fine-grained permission claim constants for sub-admins.
/// Full admins (role = "Admin") bypass all permission checks.
/// Claims use type <see cref="ClaimType"/> with one of the values below.
/// </summary>
public static class AdminPermissions
{
    public const string ClaimType = "admin:permission";

    public const string ManageSchedule = "manage-schedule";
    public const string ManageRegistrations = "manage-registrations";
    public const string ManageLeagues = "manage-leagues";
    public const string ManageRinks = "manage-rinks";
    public const string ViewReports = "view-reports";

    public static readonly IReadOnlyList<string> All = new[]
    {
        ManageSchedule,
        ManageRegistrations,
        ManageLeagues,
        ManageRinks,
        ViewReports,
    };
}
