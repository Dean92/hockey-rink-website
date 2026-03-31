namespace HockeyRinkAPI.Models.Responses;

public class AdminUserSummaryDto
{
    public string Id { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsFullAdmin { get; set; }
    public bool IsSubAdmin { get; set; }
    public List<string> Permissions { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public class UserSearchResultDto
{
    public string Id { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OperationResult
{
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public static OperationResult Ok() => new() { Succeeded = true };
    public static OperationResult Fail(string error) => new() { Succeeded = false, ErrorMessage = error };
}
