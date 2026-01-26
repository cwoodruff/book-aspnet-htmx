using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Services;

public class ArtistService : IArtistService
{
    private readonly ChinookContext _context;

    public ArtistService(ChinookContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<Artist>> GetArtistsAsync(string? search, int page, int pageSize)
    {
        var query = _context.Artists.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Name.Contains(search));
        }

        query = query.OrderBy(a => a.Name);

        return await PaginatedList<Artist>.CreateAsync(query, page, pageSize);
    }

    public async Task<Artist?> GetByIdAsync(int id)
    {
        return await _context.Artists
            .Include(a => a.Albums)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Artist> CreateAsync(string name)
    {
        var artist = new Artist { Name = name };
        _context.Artists.Add(artist);
        await _context.SaveChangesAsync();
        return artist;
    }

    public async Task<Artist?> UpdateAsync(int id, string name)
    {
        var artist = await _context.Artists.FindAsync(id);
        if (artist == null) return null;

        artist.Name = name;
        await _context.SaveChangesAsync();
        return artist;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var artist = await _context.Artists.FindAsync(id);
        if (artist == null) return false;

        _context.Artists.Remove(artist);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Artists.CountAsync();
    }

    public async Task<IEnumerable<Artist>> GetTopArtistsAsync(int count)
    {
        return await _context.Artists
            .Include(a => a.Albums)
            .OrderByDescending(a => a.Albums.Count)
            .Take(count)
            .ToListAsync();
    }
}
