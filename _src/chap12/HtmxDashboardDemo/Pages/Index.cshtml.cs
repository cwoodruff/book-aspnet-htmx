using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    public IActionResult OnGetOverview()
    {
        var html = "<h3>Overview</h3><p>This is your dashboard overview with quick stats and summaries.</p>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetReports()
    {
        var html = "<h3>Reports</h3><p>Detailed reports and analytics will appear here.</p>";
        return Content(html, "text/html");
    }

    public IActionResult OnGetSettings()
    {
        var html = "<h3>Settings</h3><p>Manage your user settings and preferences from this panel.</p>";
        return Content(html, "text/html");
    }
}