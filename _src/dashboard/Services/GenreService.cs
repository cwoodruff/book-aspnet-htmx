using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Services;

public class GenreService : IGenreService
{
    private readonly ChinookContext _context;

    public GenreService(ChinookContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Genre>> GetAllAsync()
    {
        return await _context.Genres
            .OrderBy(g => g.Name)
            .ToListAsync();
    }

    public async Task<Genre?> GetByIdAsync(int id)
    {
        return await _context.Genres.FindAsync(id);
    }

    public async Task<int> GetTrackCountAsync(int genreId)
    {
        return await _context.Tracks
            .CountAsync(t => t.GenreId == genreId);
    }
}
