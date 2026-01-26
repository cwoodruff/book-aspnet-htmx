namespace ChinookDashboard.Models;

public class GenreDistribution
{
    public string GenreName { get; set; } = string.Empty;
    public int TrackCount { get; set; }
    public decimal Percentage { get; set; }
    public string Color { get; set; } = string.Empty;
}
