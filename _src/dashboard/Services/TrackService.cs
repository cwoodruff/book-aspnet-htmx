using ChinookDashboard.Data;
using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Services;

public class TrackService : ITrackService
{
    private readonly ChinookContext _context;

    public TrackService(ChinookContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<Track>> GetTracksAsync(string? search, int? genreId, int? albumId, int page, int pageSize)
    {
        var query = _context.Tracks
            .Include(t => t.Album)
                .ThenInclude(a => a!.Artist)
            .Include(t => t.Genre)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search) || 
                (t.Composer != null && t.Composer.Contains(search)));
        }

        if (genreId.HasValue)
        {
            query = query.Where(t => t.GenreId == genreId.Value);
        }

        if (albumId.HasValue)
        {
            query = query.Where(t => t.AlbumId == albumId.Value);
        }

        query = query.OrderBy(t => t.Name);

        return await PaginatedList<Track>.CreateAsync(query, page, pageSize);
    }

    public async Task<Track?> GetByIdAsync(int id)
    {
        return await _context.Tracks
            .Include(t => t.Album)
                .ThenInclude(a => a!.Artist)
            .Include(t => t.Genre)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<Track> CreateAsync(string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice)
    {
        var track = new Track
        {
            Name = name,
            AlbumId = albumId,
            GenreId = genreId,
            Composer = composer,
            Milliseconds = milliseconds,
            UnitPrice = unitPrice
        };
        
        _context.Tracks.Add(track);
        await _context.SaveChangesAsync();
        
        // Reload with related entities
        await _context.Entry(track).Reference(t => t.Album).LoadAsync();
        if (track.Album != null)
            await _context.Entry(track.Album).Reference(a => a.Artist).LoadAsync();
        await _context.Entry(track).Reference(t => t.Genre).LoadAsync();
        
        return track;
    }

    public async Task<Track?> UpdateAsync(int id, string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice)
    {
        var track = await _context.Tracks.FindAsync(id);
        if (track == null) return null;

        track.Name = name;
        track.AlbumId = albumId;
        track.GenreId = genreId;
        track.Composer = composer;
        track.Milliseconds = milliseconds;
        track.UnitPrice = unitPrice;
        
        await _context.SaveChangesAsync();
        
        // Reload with related entities
        await _context.Entry(track).Reference(t => t.Album).LoadAsync();
        if (track.Album != null)
            await _context.Entry(track.Album).Reference(a => a.Artist).LoadAsync();
        await _context.Entry(track).Reference(t => t.Genre).LoadAsync();
        
        return track;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var track = await _context.Tracks.FindAsync(id);
        if (track == null) return false;

        _context.Tracks.Remove(track);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BulkDeleteAsync(IEnumerable<int> ids)
    {
        var tracks = await _context.Tracks
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
        
        if (!tracks.Any()) return false;

        _context.Tracks.RemoveRange(tracks);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> BulkUpdateGenreAsync(IEnumerable<int> ids, int genreId)
    {
        var tracks = await _context.Tracks
            .Where(t => ids.Contains(t.Id))
            .ToListAsync();
        
        if (!tracks.Any()) return false;

        foreach (var track in tracks)
        {
            track.GenreId = genreId;
        }
        
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Tracks.CountAsync();
    }

    public async Task<IEnumerable<TrackSummary>> GetRecentTracksAsync(int count)
    {
        return await _context.Tracks
            .Include(t => t.Album)
                .ThenInclude(a => a!.Artist)
            .Include(t => t.Genre)
            .OrderByDescending(t => t.Id)
            .Take(count)
            .Select(t => new TrackSummary
            {
                Id = t.Id,
                Name = t.Name,
                ArtistName = t.Album != null ? t.Album.Artist.Name : "Unknown",
                AlbumTitle = t.Album != null ? t.Album.Title : "Unknown",
                GenreName = t.Genre != null ? t.Genre.Name : "Unknown",
                Milliseconds = t.Milliseconds,
                UnitPrice = t.UnitPrice
            })
            .ToListAsync();
    }
}
