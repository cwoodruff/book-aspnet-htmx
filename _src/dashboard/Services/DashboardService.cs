using ChinookDashboard.Data;
using ChinookDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ChinookDashboard.Services;

public class DashboardService : IDashboardService
{
    private readonly ChinookContext _context;
    
    private static readonly string[] ChartColors = new[]
    {
        "#3b82f6", "#ef4444", "#22c55e", "#f59e0b", "#8b5cf6",
        "#ec4899", "#06b6d4", "#84cc16", "#f97316", "#6366f1"
    };

    public DashboardService(ChinookContext context)
    {
        _context = context;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        var totalArtists = await _context.Artists.CountAsync();
        var totalAlbums = await _context.Albums.CountAsync();
        var totalTracks = await _context.Tracks.CountAsync();
        var totalGenres = await _context.Genres.CountAsync();
        var totalMilliseconds = await _context.Tracks.SumAsync(t => (long)t.Milliseconds);
        var totalValue = await _context.Tracks.SumAsync(t => t.UnitPrice);

        var duration = TimeSpan.FromMilliseconds(totalMilliseconds);
        var totalDuration = $"{(int)duration.TotalHours}h {duration.Minutes}m";

        return new DashboardStats
        {
            TotalArtists = totalArtists,
            TotalAlbums = totalAlbums,
            TotalTracks = totalTracks,
            TotalGenres = totalGenres,
            TotalDuration = totalDuration,
            TotalValue = totalValue
        };
    }

    public async Task<IEnumerable<GenreDistribution>> GetGenreDistributionAsync()
    {
        var totalTracks = await _context.Tracks.CountAsync();
        
        var distribution = await _context.Tracks
            .Where(t => t.GenreId != null)
            .GroupBy(t => t.Genre!.Name)
            .Select(g => new
            {
                GenreName = g.Key,
                TrackCount = g.Count()
            })
            .OrderByDescending(g => g.TrackCount)
            .Take(10)
            .ToListAsync();

        return distribution.Select((g, index) => new GenreDistribution
        {
            GenreName = g.GenreName,
            TrackCount = g.TrackCount,
            Percentage = totalTracks > 0 ? Math.Round((decimal)g.TrackCount / totalTracks * 100, 1) : 0,
            Color = ChartColors[index % ChartColors.Length]
        });
    }
}
