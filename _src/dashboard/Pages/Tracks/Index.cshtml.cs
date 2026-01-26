using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using ChinookDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ChinookDashboard.Pages.Tracks;

public class IndexModel : PageModel
{
    private readonly ITrackService _trackService;
    private readonly IGenreService _genreService;
    private readonly IAlbumService _albumService;
    private const int PageSize = 20;

    public IndexModel(ITrackService trackService, IGenreService genreService, IAlbumService albumService)
    {
        _trackService = trackService;
        _genreService = genreService;
        _albumService = albumService;
    }

    public PaginatedList<Track> Tracks { get; set; } = null!;
    public IEnumerable<Genre> Genres { get; set; } = Enumerable.Empty<Genre>();
    public IEnumerable<Album> Albums { get; set; } = Enumerable.Empty<Album>();
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? GenreId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? AlbumId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Tracks = await _trackService.GetTracksAsync(Search, GenreId, AlbumId, Page, PageSize);
        Genres = await _genreService.GetAllAsync();
    }

    public async Task<IActionResult> OnGetListAsync()
    {
        Tracks = await _trackService.GetTracksAsync(Search, GenreId, AlbumId, Page, PageSize);
        
        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            updateCount = new { count = Tracks.TotalCount, start = Tracks.StartItem, end = Tracks.EndItem }
        }));
        
        return Partial("Tracks/_TrackList", Tracks);
    }

    public async Task<IActionResult> OnGetEditAsync(int id)
    {
        var track = await _trackService.GetByIdAsync(id);
        if (track == null) return NotFound();
        
        Genres = await _genreService.GetAllAsync();
        ViewData["Genres"] = Genres;
        
        return Partial("Tracks/_TrackEditRow", track);
    }

    public async Task<IActionResult> OnGetCancelEditAsync(int id)
    {
        var track = await _trackService.GetByIdAsync(id);
        if (track == null) return NotFound();
        
        return Partial("Tracks/_TrackRow", track);
    }

    public async Task<IActionResult> OnGetCreateFormAsync()
    {
        Genres = await _genreService.GetAllAsync();
        Albums = await _albumService.GetAlbumsAsync(null, null, 1, 1000);
        ViewData["Genres"] = Genres;
        ViewData["Albums"] = Albums;
        return Partial("Tracks/_TrackForm", new Track());
    }

    public async Task<IActionResult> OnPostCreateAsync(string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required");
        }

        var track = await _trackService.CreateAsync(name, albumId, genreId, composer, milliseconds, unitPrice);

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            trackCreated = new { id = track.Id },
            showToast = new { message = $"Track '{track.Name}' created successfully", type = "success" },
            closeModal = true
        }));

        return Partial("Tracks/_TrackRow", track);
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string name, int? albumId, int? genreId, string? composer, int milliseconds, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required");
        }

        var track = await _trackService.UpdateAsync(id, name, albumId, genreId, composer, milliseconds, unitPrice);
        if (track == null) return NotFound();

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            showToast = new { message = $"Track '{track.Name}' updated successfully", type = "success" }
        }));

        return Partial("Tracks/_TrackRow", track);
    }

    public async Task<IActionResult> OnDeleteAsync(int id)
    {
        var track = await _trackService.GetByIdAsync(id);
        if (track == null) return NotFound();

        var deleted = await _trackService.DeleteAsync(id);
        if (!deleted) return BadRequest("Could not delete track");

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            trackDeleted = new { id = id },
            showToast = new { message = $"Track '{track.Name}' deleted successfully", type = "success" }
        }));

        return Content("");
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync([FromForm] int[] selectedTracks)
    {
        if (selectedTracks == null || selectedTracks.Length == 0)
        {
            return BadRequest("No tracks selected");
        }

        var deleted = await _trackService.BulkDeleteAsync(selectedTracks);
        if (!deleted) return BadRequest("Could not delete tracks");

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            tracksDeleted = new { ids = selectedTracks },
            showToast = new { message = $"{selectedTracks.Length} tracks deleted successfully", type = "success" }
        }));

        return Content("");
    }

    public async Task<IActionResult> OnPostBulkUpdateGenreAsync([FromForm] int[] selectedTracks, [FromForm] int genreId)
    {
        if (selectedTracks == null || selectedTracks.Length == 0)
        {
            return BadRequest("No tracks selected");
        }

        var updated = await _trackService.BulkUpdateGenreAsync(selectedTracks, genreId);
        if (!updated) return BadRequest("Could not update tracks");

        var genre = await _genreService.GetByIdAsync(genreId);

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            tracksUpdated = new { ids = selectedTracks, genreId = genreId },
            showToast = new { message = $"{selectedTracks.Length} tracks moved to {genre?.Name ?? "Unknown"}", type = "success" }
        }));

        // Refresh the list
        Tracks = await _trackService.GetTracksAsync(Search, GenreId, AlbumId, Page, PageSize);
        return Partial("Tracks/_TrackList", Tracks);
    }
}
