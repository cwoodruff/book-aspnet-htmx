using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;

namespace ChinookDashboard.Services;

public interface IArtistService
{
    Task<PaginatedList<Artist>> GetArtistsAsync(string? search, int page, int pageSize);
    Task<Artist?> GetByIdAsync(int id);
    Task<Artist> CreateAsync(string name);
    Task<Artist?> UpdateAsync(int id, string name);
    Task<bool> DeleteAsync(int id);
    Task<int> GetCountAsync();
    Task<IEnumerable<Artist>> GetTopArtistsAsync(int count);
}
