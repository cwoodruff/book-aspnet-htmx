using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using ChinookDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChinookDashboard.Pages;

public class IndexModel : PageModel
{
    private readonly IDashboardService _dashboardService;
    private readonly IArtistService _artistService;
    private readonly ITrackService _trackService;

    public IndexModel(
        IDashboardService dashboardService,
        IArtistService artistService,
        ITrackService trackService)
    {
        _dashboardService = dashboardService;
        _artistService = artistService;
        _trackService = trackService;
    }

    public DashboardStats Stats { get; set; } = new();
    public IEnumerable<GenreDistribution> GenreDistribution { get; set; } = Enumerable.Empty<GenreDistribution>();
    public IEnumerable<TrackSummary> RecentTracks { get; set; } = Enumerable.Empty<TrackSummary>();
    public IEnumerable<Artist> TopArtists { get; set; } = Enumerable.Empty<Artist>();

    public async Task OnGetAsync()
    {
        Stats = await _dashboardService.GetStatsAsync();
        GenreDistribution = await _dashboardService.GetGenreDistributionAsync();
        RecentTracks = await _trackService.GetRecentTracksAsync(5);
        TopArtists = await _artistService.GetTopArtistsAsync(5);
    }

    public async Task<IActionResult> OnGetStatsAsync()
    {
        Stats = await _dashboardService.GetStatsAsync();
        return Partial("Dashboard/_StatsPanel", Stats);
    }

    public async Task<IActionResult> OnGetGenreChartAsync()
    {
        GenreDistribution = await _dashboardService.GetGenreDistributionAsync();
        return Partial("Dashboard/_GenreChart", GenreDistribution);
    }

    public async Task<IActionResult> OnGetRecentTracksAsync()
    {
        RecentTracks = await _trackService.GetRecentTracksAsync(5);
        return Partial("Dashboard/_RecentTracks", RecentTracks);
    }

    public async Task<IActionResult> OnGetTopArtistsAsync()
    {
        TopArtists = await _artistService.GetTopArtistsAsync(5);
        return Partial("Dashboard/_TopArtists", TopArtists);
    }
}
