using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxValidation.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {
    }
    
    public IActionResult OnGetCheckUsername(string username)
    {
        var takenUsernames = new[] { "admin", "user1", "guest" };

        if (string.IsNullOrWhiteSpace(username))
        {
            return Content("<span style='color: red;'>Username is required.</span>", "text/html");
        }

        if (takenUsernames.Contains(username, StringComparer.OrdinalIgnoreCase))
        {
            return Content("<span style='color: red;'>That username is already taken.</span>", "text/html");
        }

        return Content("<span style='color: green;'>Username is available!</span>", "text/html");
    }
}