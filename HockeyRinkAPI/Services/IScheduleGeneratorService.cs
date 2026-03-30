using HockeyRinkAPI.Models.Requests;
using HockeyRinkAPI.Models.Responses;

namespace HockeyRinkAPI.Services;

public interface IScheduleGeneratorService
{
    /// <summary>
    /// Generates a proposed round-robin schedule without saving anything to the database.
    /// The admin reviews the result and calls ConfirmAsync to persist it.
    /// </summary>
    Task<GenerateScheduleResponse> GenerateAsync(GenerateScheduleRequest request);
}
