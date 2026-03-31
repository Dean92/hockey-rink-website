namespace HockeyRinkAPI.Models;

public static class PositionConstants
{
    public const string Goalie = "Goalie";
    public const string Forward = "Forward";
    public const string Defense = "Defense";
    public const string ForwardDefense = "Forward/Defense";

    public static readonly IReadOnlyList<string> All = [Goalie, Forward, Defense, ForwardDefense];
}
