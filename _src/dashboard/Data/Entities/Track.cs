namespace ChinookDashboard.Data.Entities;

public class Track
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AlbumId { get; set; }
    public int? GenreId { get; set; }
    public string? Composer { get; set; }
    public int Milliseconds { get; set; }
    public int? Bytes { get; set; }
    public decimal UnitPrice { get; set; }
    
    public Album? Album { get; set; }
    public Genre? Genre { get; set; }
    
    public string Duration => TimeSpan.FromMilliseconds(Milliseconds).ToString(@"m\:ss");
}
