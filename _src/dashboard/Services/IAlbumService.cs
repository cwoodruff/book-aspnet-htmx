using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;

namespace ChinookDashboard.Services;

public interface IAlbumService
{
    Task<PaginatedList<Album>> GetAlbumsAsync(string? search, int? artistId, int page, int pageSize);
    Task<Album?> GetByIdAsync(int id);
    Task<Album?> GetByIdWithTracksAsync(int id);
    Task<Album> CreateAsync(string title, int artistId);
    Task<Album?> UpdateAsync(int id, string title, int artistId);
    Task<bool> DeleteAsync(int id);
    Task<int> GetCountAsync();
    Task<IEnumerable<Album>> GetByArtistAsync(int artistId);
}
