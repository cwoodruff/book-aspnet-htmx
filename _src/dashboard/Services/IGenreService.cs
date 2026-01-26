using ChinookDashboard.Data.Entities;

namespace ChinookDashboard.Services;

public interface IGenreService
{
    Task<IEnumerable<Genre>> GetAllAsync();
    Task<Genre?> GetByIdAsync(int id);
    Task<int> GetTrackCountAsync(int genreId);
}
