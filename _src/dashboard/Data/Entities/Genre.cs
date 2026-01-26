namespace ChinookDashboard.Data.Entities;

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    public ICollection<Track> Tracks { get; set; } = new List<Track>();
}
