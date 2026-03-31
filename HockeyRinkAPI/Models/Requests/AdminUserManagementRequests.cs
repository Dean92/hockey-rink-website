namespace HockeyRinkAPI.Models.Requests;

public class GrantAdminRequest
{
    public List<string> Permissions { get; set; } = new();
}

public class UpdatePermissionsRequest
{
    public List<string> Permissions { get; set; } = new();
}

public class InviteAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
}
