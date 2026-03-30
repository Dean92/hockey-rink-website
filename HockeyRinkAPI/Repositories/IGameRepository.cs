using HockeyRinkAPI.Models;

namespace HockeyRinkAPI.Repositories;

public interface IGameRepository
{
    Task<List<Game>> GetLeagueGamesAsync(
        int leagueId,
        int? teamId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? rinkId = null);

    Task<Game?> GetByIdAsync(int id);
    Task<Game> UpdateAsync(Game game);
    Task CancelAsync(int id);
}
