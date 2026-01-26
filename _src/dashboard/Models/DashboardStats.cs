namespace ChinookDashboard.Models;

public class DashboardStats
{
    public int TotalArtists { get; set; }
    public int TotalAlbums { get; set; }
    public int TotalTracks { get; set; }
    public int TotalGenres { get; set; }
    public string TotalDuration { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
}
