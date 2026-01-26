using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Services;

public class AlbumService : IAlbumService
{
    private readonly ChinookContext _context;

    public AlbumService(ChinookContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<Album>> GetAlbumsAsync(string? search, int? artistId, int page, int pageSize)
    {
        var query = _context.Albums
            .Include(a => a.Artist)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Title.Contains(search) || a.Artist.Name.Contains(search));
        }

        if (artistId.HasValue)
        {
            query = query.Where(a => a.ArtistId == artistId.Value);
        }

        query = query.OrderBy(a => a.Title);

        return await PaginatedList<Album>.CreateAsync(query, page, pageSize);
    }

    public async Task<Album?> GetByIdAsync(int id)
    {
        return await _context.Albums
            .Include(a => a.Artist)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Album?> GetByIdWithTracksAsync(int id)
    {
        return await _context.Albums
            .Include(a => a.Artist)
            .Include(a => a.Tracks)
                .ThenInclude(t => t.Genre)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Album> CreateAsync(string title, int artistId)
    {
        var album = new Album { Title = title, ArtistId = artistId };
        _context.Albums.Add(album);
        await _context.SaveChangesAsync();
        
        // Reload with artist
        await _context.Entry(album).Reference(a => a.Artist).LoadAsync();
        return album;
    }

    public async Task<Album?> UpdateAsync(int id, string title, int artistId)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album == null) return null;

        album.Title = title;
        album.ArtistId = artistId;
        await _context.SaveChangesAsync();
        
        await _context.Entry(album).Reference(a => a.Artist).LoadAsync();
        return album;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var album = await _context.Albums.FindAsync(id);
        if (album == null) return false;

        _context.Albums.Remove(album);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Albums.CountAsync();
    }

    public async Task<IEnumerable<Album>> GetByArtistAsync(int artistId)
    {
        return await _context.Albums
            .Include(a => a.Artist)
            .Where(a => a.ArtistId == artistId)
            .OrderBy(a => a.Title)
            .ToListAsync();
    }
}
