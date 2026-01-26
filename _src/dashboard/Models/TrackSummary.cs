namespace ChinookDashboard.Models;

public class TrackSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ArtistName { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public string GenreName { get; set; } = string.Empty;
    public int Milliseconds { get; set; }
    public decimal UnitPrice { get; set; }
    
    public string Duration => TimeSpan.FromMilliseconds(Milliseconds).ToString(@"m\:ss");
}
