using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using ChinookDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ChinookDashboard.Pages.Albums;

public class IndexModel : PageModel
{
    private readonly IAlbumService _albumService;
    private readonly IArtistService _artistService;
    private const int PageSize = 12;

    public IndexModel(IAlbumService albumService, IArtistService artistService)
    {
        _albumService = albumService;
        _artistService = artistService;
    }

    public PaginatedList<Album> Albums { get; set; } = null!;
    public IEnumerable<Artist> AllArtists { get; set; } = Enumerable.Empty<Artist>();
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int? ArtistId { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Albums = await _albumService.GetAlbumsAsync(Search, ArtistId, Page, PageSize);
        AllArtists = (await _artistService.GetArtistsAsync(null, 1, 1000)).ToList();
    }

    public async Task<IActionResult> OnGetGridAsync()
    {
        Albums = await _albumService.GetAlbumsAsync(Search, ArtistId, Page, PageSize);
        
        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            updateCount = new { count = Albums.TotalCount, start = Albums.StartItem, end = Albums.EndItem }
        }));
        
        return Partial("Albums/_AlbumGrid", Albums);
    }

    public async Task<IActionResult> OnGetDetailAsync(int id)
    {
        var album = await _albumService.GetByIdWithTracksAsync(id);
        if (album == null) return NotFound();
        
        return Partial("Albums/_AlbumDetail", album);
    }

    public async Task<IActionResult> OnGetCreateFormAsync()
    {
        AllArtists = (await _artistService.GetArtistsAsync(null, 1, 1000)).ToList();
        ViewData["Artists"] = AllArtists;
        return Partial("Albums/_AlbumForm", new Album());
    }

    public async Task<IActionResult> OnGetEditFormAsync(int id)
    {
        var album = await _albumService.GetByIdAsync(id);
        if (album == null) return NotFound();
        
        AllArtists = (await _artistService.GetArtistsAsync(null, 1, 1000)).ToList();
        ViewData["Artists"] = AllArtists;
        return Partial("Albums/_AlbumForm", album);
    }

    public async Task<IActionResult> OnPostCreateAsync(string title, int artistId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Title is required");
        }

        var album = await _albumService.CreateAsync(title, artistId);

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            albumCreated = new { id = album.Id },
            showToast = new { message = $"Album '{album.Title}' created successfully", type = "success" },
            closeModal = true
        }));

        return Partial("Albums/_AlbumCard", album);
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string title, int artistId)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return BadRequest("Title is required");
        }

        var album = await _albumService.UpdateAsync(id, title, artistId);
        if (album == null) return NotFound();

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            showToast = new { message = $"Album '{album.Title}' updated successfully", type = "success" },
            closeModal = true
        }));

        return Partial("Albums/_AlbumCard", album);
    }

    public async Task<IActionResult> OnDeleteAsync(int id)
    {
        var album = await _albumService.GetByIdAsync(id);
        if (album == null) return NotFound();

        var deleted = await _albumService.DeleteAsync(id);
        if (!deleted) return BadRequest("Could not delete album");

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            albumDeleted = new { id = id },
            showToast = new { message = $"Album '{album.Title}' deleted successfully", type = "success" }
        }));

        return Content("");
    }

    public async Task<IActionResult> OnGetTrackListAsync(int id)
    {
        var album = await _albumService.GetByIdWithTracksAsync(id);
        if (album == null) return NotFound();

        return Partial("Albums/_AlbumTrackList", album);
    }
}
