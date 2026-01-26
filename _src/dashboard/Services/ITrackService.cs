using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;

namespace ChinookDashboard.Services;

public interface ITrackService
{
    Task<PaginatedList<Track>> GetTracksAsync(string? search, int? genreId, int? albumId, int page, int pageSize);
    Task<Track?> GetByIdAsync(int id);
    Task<Track> CreateAsync(string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice);
    Task<Track?> UpdateAsync(int id, string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice);
    Task<bool> DeleteAsync(int id);
    Task<bool> BulkDeleteAsync(IEnumerable<int> ids);
    Task<bool> BulkUpdateGenreAsync(IEnumerable<int> ids, int genreId);
    Task<int> GetCountAsync();
    Task<IEnumerable<TrackSummary>> GetRecentTracksAsync(int count);
}
