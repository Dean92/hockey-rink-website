using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;

namespace HockeyRinkAPI.Services;

public interface IPlayoffSchedulerService
{
    /// <summary>
    /// Generates a single-elimination playoff bracket preview.
    /// Seeds teams by regular-season win-loss record (ties broken alphabetically).
    /// Every team qualifies; non-power-of-2 team counts receive first-round byes.
    /// No database writes — caller decides whether to confirm.
    /// </summary>
    Task<GenerateScheduleResponse> GenerateAsync(GeneratePlayoffRequest req);
}
