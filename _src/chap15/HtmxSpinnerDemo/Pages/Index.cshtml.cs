using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HtmxSpinnerDemo.Pages;

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

    public IActionResult OnGetSearch(string query)
    {
        // Simulate delay for loading spinner demonstration
        System.Threading.Thread.Sleep(5000);

        if (string.IsNullOrWhiteSpace(query))
        {
            return Content("<div>Please enter a search query.</div>", "text/html");
        }

        var resultHtml = $@"
            <div>
                Results for '<strong>{query}</strong>':
                <ul>
                    <li>User 1 matching '{query}'</li>
                    <li>User 2 matching '{query}'</li>
                    <li>User 3 matching '{query}'</li>
                </ul>
            </div>";

        return Content(resultHtml, "text/html");
    }
}