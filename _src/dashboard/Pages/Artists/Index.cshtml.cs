using ChinookDashboard.Data.Entities;
using ChinookDashboard.Models;
using ChinookDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace ChinookDashboard.Pages.Artists;

public class IndexModel : PageModel
{
    private readonly IArtistService _artistService;
    private const int PageSize = 10;

    public IndexModel(IArtistService artistService)
    {
        _artistService = artistService;
    }

    public PaginatedList<Artist> Artists { get; set; } = null!;
    
    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int Page { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Artists = await _artistService.GetArtistsAsync(Search, Page, PageSize);
    }

    public async Task<IActionResult> OnGetListAsync()
    {
        Artists = await _artistService.GetArtistsAsync(Search, Page, PageSize);
        
        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            updateCount = new { count = Artists.TotalCount, start = Artists.StartItem, end = Artists.EndItem }
        }));
        
        return Partial("Artists/_ArtistList", Artists);
    }

    public async Task<IActionResult> OnGetEditAsync(int id)
    {
        var artist = await _artistService.GetByIdAsync(id);
        if (artist == null) return NotFound();
        
        return Partial("Artists/_ArtistEditRow", artist);
    }

    public async Task<IActionResult> OnGetCancelEditAsync(int id)
    {
        var artist = await _artistService.GetByIdAsync(id);
        if (artist == null) return NotFound();
        
        return Partial("Artists/_ArtistRow", artist);
    }

    public IActionResult OnGetCreateForm()
    {
        return Partial("Artists/_ArtistForm", new Artist());
    }

    public async Task<IActionResult> OnPostCreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required");
        }

        var artist = await _artistService.CreateAsync(name);
        var count = await _artistService.GetCountAsync();

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            artistCreated = new { id = artist.Id, name = artist.Name },
            showToast = new { message = $"Artist '{artist.Name}' created successfully", type = "success" },
            closeModal = true
        }));

        return Partial("Artists/_ArtistRow", artist);
    }

    public async Task<IActionResult> OnPostUpdateAsync(int id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest("Name is required");
        }

        var artist = await _artistService.UpdateAsync(id, name);
        if (artist == null) return NotFound();

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            showToast = new { message = $"Artist '{artist.Name}' updated successfully", type = "success" }
        }));

        return Partial("Artists/_ArtistRow", artist);
    }

    public async Task<IActionResult> OnDeleteAsync(int id)
    {
        var artist = await _artistService.GetByIdAsync(id);
        if (artist == null) return NotFound();

        var deleted = await _artistService.DeleteAsync(id);
        if (!deleted) return BadRequest("Could not delete artist");

        var count = await _artistService.GetCountAsync();

        Response.Headers.Append("HX-Trigger", JsonSerializer.Serialize(new
        {
            artistDeleted = new { id = id },
            showToast = new { message = $"Artist '{artist.Name}' deleted successfully", type = "success" },
            updateCount = new { count = count }
        }));

        return Content("");
    }

    public async Task<IActionResult> OnGetConfirmDeleteAsync(int id)
    {
        var artist = await _artistService.GetByIdAsync(id);
        if (artist == null) return NotFound();

        return Partial("Artists/_ConfirmDeleteArtist", artist);
    }
}
